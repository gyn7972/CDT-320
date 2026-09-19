using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.Common.Alarms;
using QMC.Common.Diagnostics.TactTime;
using QMC.CDT320.Bin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Jobs;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Alarms;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Initialization;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.Recipes;

namespace QMC.CDT320
{
    public partial class MachineController
    {
        public void SetActiveRecipeName(string recipeName)
        {
            string previousRecipeName = ActiveRecipeName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(recipeName))
            {
                ActiveRecipeName = string.Empty;
                return;
            }

            string normalized = recipeName.Trim();
            string nextRecipeName = string.Equals(normalized, "-", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : normalized;
            ActiveRecipeName = nextRecipeName;

            if (!string.Equals(previousRecipeName, nextRecipeName, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(nextRecipeName))
            {
                InvalidateRecipeRunReadiness();

                string reason = string.IsNullOrWhiteSpace(previousRecipeName)
                    ? "InitialRecipe:" + nextRecipeName
                    : "RecipeChange:" + previousRecipeName + "->" + nextRecipeName;
                RequestOutputFullPreparation(reason, nextRecipeName);
            }
        }

        public void InvalidateRecipeRunReadiness()
        {
            lock (_recipeRunGateLock)
            {
                _recipeRunGateVerifiedName = string.Empty;
                _recipeRunGateVerifiedAtUtc = DateTime.MinValue;
            }
        }

        public bool TryValidateRecipeChange(
            string recipeName,
            out bool materialRecipeRestore,
            out string reason)
        {
            bool ignoredMaterialOnlyBlock;
            return TryValidateRecipeChange(
                recipeName,
                out materialRecipeRestore,
                out ignoredMaterialOnlyBlock,
                out reason);
        }

        /// <summary>
        /// [강제 Recipe 변경 2026-08-09] 차단된 경우 그것이 "Material 데이터 잔재만"인지 알려준다.
        /// materialOnlyBlock 이 true 면 물리 센서 제품 감지가 없다는 뜻이므로,
        /// 작업자 확인을 받아 <see cref="ForceClearInMachineMaterial"/> 후 재시도할 수 있다.
        /// 물리 센서가 제품을 감지했거나 Alarm/동작 중 차단이면 false 이며 강제 변경 대상이 아니다.
        /// </summary>
        public bool TryValidateRecipeChange(
            string recipeName,
            out bool materialRecipeRestore,
            out bool materialOnlyBlock,
            out string reason)
        {
            return TryValidateRecipeChangeCore(
                recipeName,
                false,
                false,
                out materialRecipeRestore,
                out materialOnlyBlock,
                out reason);
        }

        /// <summary>
        /// [강제 Recipe 변경 2026-08-09] 작업자가 "장비 안이 비어 있다"고 확인한 뒤,
        /// Recipe 변경을 막고 있던 장비 내부 Material 기록을 정리한다.
        /// 안전을 위해 호출 시점에 물리 센서 제품 감지가 없는지 다시 확인하고,
        /// 감지되면 정리하지 않는다(대화상자 확인과 실제 실행 사이의 상태 변화 방어).
        /// </summary>
        public bool ForceClearInMachineMaterial(string requestedRecipeName, out string detail)
        {
            detail = string.Empty;

            try
            {
                if (_status == EquipmentStatus.Alarm || AlarmManager.HasActive)
                {
                    detail = "Alarm 상태에서는 강제 Material 정리를 할 수 없습니다.";
                    return false;
                }

                if (HasActiveEquipmentOperation)
                {
                    detail = "장비 동작 중에는 강제 Material 정리를 할 수 없습니다.";
                    return false;
                }

                bool hasPhysicalEvidence;
                string physicalDetail;
                string physicalCheckReason;
                if (!TryCollectRecipePhysicalProductEvidence(
                        out hasPhysicalEvidence,
                        out physicalDetail,
                        out physicalCheckReason))
                {
                    detail = "실제 제품 감지 신호를 확인할 수 없어 강제 정리를 중단했습니다. " + physicalCheckReason;
                    return false;
                }

                if (hasPhysicalEvidence)
                {
                    detail = "실제 제품 감지 신호가 있어 강제 정리를 중단했습니다. physical=" + physicalDetail;
                    return false;
                }

                string materialRecipeName;
                string materialDetail;
                HasInMachineMaterial(out materialRecipeName, out materialDetail);

                string clearDetail;
                if (!MaterialStateService.ClearAllMaterialForRecipeChange(
                        "ForceRecipeChange:" + (requestedRecipeName ?? ""),
                        out clearDetail))
                {
                    detail = clearDetail;
                    return false;
                }

                detail = clearDetail;
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Alarm,
                    QMC.CDT_320.Ui.Security.UserSession.Name,
                    "RECIPE-FORCE-MATERIAL-CLEAR",
                    "작업자 확인으로 장비 내부 Material 기록을 강제 정리하고 Recipe 변경을 진행합니다. " +
                    "requested=" + (requestedRecipeName ?? "") +
                    ", materialRecipe=" + (string.IsNullOrWhiteSpace(materialRecipeName) ? "-" : materialRecipeName) +
                    ", before=" + materialDetail +
                    ", cleared=" + clearDetail);
                return true;
            }
            catch (Exception ex)
            {
                detail = "강제 Material 정리 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
        }

        public bool TryBeginRecipeApplyOperation(
            string recipeName,
            out bool materialRecipeRestore,
            out IDisposable lease,
            out string reason)
        {
            return TryBeginRecipeApplyOperation(
                recipeName,
                false,
                out materialRecipeRestore,
                out lease,
                out reason);
        }

        // To do: [시작 레시피 자동 로드] 기동 자동 적용은 알람 게이트만 면제한다.
        // 기존 조건: 시작 시 ApplyStartupMachineRuntimeState가 INIT-RESTORE-NO-STATE 등 알람을 먼저 올리고,
        //            직후의 마지막 레시피 자동 적용이 "Alarm 상태에서는 Recipe 적용 불가" 게이트에 걸려
        //            매 기동마다 조용히 실패했다(2026-08-05 로그: RECIPE-CHANGE-BLOCK → RECIPE-STARTUP-BLOCK 반복).
        // 현재 기준: startupAutoLoad일 때만 알람 게이트를 건너뛴다. 동작 중 차단·자재/레시피 정합 등
        //            나머지 게이트는 전부 그대로 검사한다.
        public bool TryBeginRecipeApplyOperation(
            string recipeName,
            bool startupAutoLoad,
            out bool materialRecipeRestore,
            out IDisposable lease,
            out string reason)
        {
            materialRecipeRestore = false;
            lease = null;
            reason = string.Empty;

            if (!TryRegisterRecipeApplyOperation(out reason))
                return false;

            bool admitted = false;
            try
            {
                bool ignoredMaterialOnlyBlockForApply;
                if (!TryValidateRecipeChangeCore(
                        recipeName,
                        true,
                        startupAutoLoad,
                        out materialRecipeRestore,
                        out ignoredMaterialOnlyBlockForApply,
                        out reason))
                    return false;

                lease = new RecipeApplyOperationLease(this);
                admitted = true;
                return true;
            }
            finally
            {
                if (!admitted)
                    EndRecipeApplyOperation();
            }
        }

        private bool TryValidateRecipeChangeCore(
            string recipeName,
            bool recipeApplyLeaseHeld,
            out bool materialRecipeRestore,
            out string reason)
        {
            bool ignoredMaterialOnlyBlock;
            return TryValidateRecipeChangeCore(
                recipeName,
                recipeApplyLeaseHeld,
                false,
                out materialRecipeRestore,
                out ignoredMaterialOnlyBlock,
                out reason);
        }

        private bool TryValidateRecipeChangeCore(
            string recipeName,
            bool recipeApplyLeaseHeld,
            bool startupAutoLoad,
            out bool materialRecipeRestore,
            out bool materialOnlyBlock,
            out string reason)
        {
            materialRecipeRestore = false;
            materialOnlyBlock = false;
            reason = string.Empty;

            string currentRecipeName = (ActiveRecipeName ?? string.Empty).Trim();
            string nextRecipeName = string.IsNullOrWhiteSpace(recipeName)
                ? string.Empty
                : recipeName.Trim();

            if (string.IsNullOrWhiteSpace(nextRecipeName))
            {
                reason = "적용할 Recipe 이름이 없습니다.";
                return false;
            }

            if (!recipeApplyLeaseHeld)
            {
                lock (_recipeOperationLock)
                {
                    if (_recipeApplyOperationActive || _inputMapModeSaveActive)
                    {
                        reason = "다른 Recipe 저장/적용 작업이 이미 진행 중입니다.";
                        return false;
                    }

                    if (_recipeStartAttemptCts != null)
                    {
                        reason =
                            "START 준비 작업이 진행 중이므로 Recipe 저장/적용을 차단했습니다. " +
                            "START 완료 또는 STOP 후 다시 실행하십시오.";
                        return false;
                    }
                }
            }

            if (_status == EquipmentStatus.Alarm || AlarmManager.HasActive)
            {
                // 현재 기준: 기동 자동 적용은 이 게이트만 면제한다 — 시작 절차가 스스로 올린 알람
                //            (INIT-RESTORE-NO-STATE 등) 때문에 마지막 레시피 적용이 막히는 자기 잠금 방지.
                //            레시피 적용은 데이터 반영이라 모션이 없고, 동작 중 차단은 아래 게이트가 계속 담당한다.
                if (startupAutoLoad)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "RecipeStartupAutoLoad",
                        "기동 자동 적용이므로 알람 게이트를 면제합니다. status=" + _status +
                        ", hasActiveAlarm=" + AlarmManager.HasActive +
                        ", requested=" + nextRecipeName + " - Check");
                }
                else
                {
                    reason =
                        "Alarm 상태에서는 Recipe를 저장하거나 적용할 수 없습니다. " +
                        "알람 원인을 조치하고 RESET 완료 후 다시 실행하십시오.";
                    return false;
                }
            }

            // 동일 이름 Recipe도 디스크의 Teaching/Offset 값이 변경됐을 수 있다.
            // 이름 비교보다 먼저 모든 운전 상태를 확인하여 동작 중 재적용을 중앙 차단한다.
            if (HasActiveEquipmentOperation)
            {
                reason =
                    "장비 동작 중에는 Recipe를 변경하거나 다시 적용할 수 없습니다. " +
                    "Auto/Manual/READY/초기화 동작을 완전히 정지한 후 다시 실행하십시오. " +
                    "active=" + currentRecipeName + ", requested=" + nextRecipeName;
                return false;
            }

            string materialRecipeName;
            string materialDetail;
            bool hasMaterial = HasInMachineMaterial(out materialRecipeName, out materialDetail);

            bool hasPhysicalEvidence = false;
            string physicalDetail = string.Empty;
            if (!string.IsNullOrWhiteSpace(currentRecipeName))
            {
                string physicalCheckReason;
                if (!TryCollectRecipePhysicalProductEvidence(
                        out hasPhysicalEvidence,
                        out physicalDetail,
                        out physicalCheckReason))
                {
                    reason =
                        "실제 제품 감지 신호를 확인할 수 없어 Recipe 적용을 차단했습니다. " +
                        physicalCheckReason +
                        " active=" + currentRecipeName +
                        ", requested=" + nextRecipeName;
                    return false;
                }
            }

            bool sameRecipe = string.Equals(
                currentRecipeName,
                nextRecipeName,
                StringComparison.OrdinalIgnoreCase);
            // [강제 변경 2026-08-09] 물리 센서 근거가 없고 Material 데이터 잔재만 남은 경우에 한해
            // 작업자 확인을 거친 강제 변경을 허용한다(실제 제품 감지 시에는 절대 허용하지 않는다).
            materialOnlyBlock = hasMaterial && !hasPhysicalEvidence;

            if (sameRecipe)
            {
                if (!hasMaterial && !hasPhysicalEvidence)
                    return true;

                reason =
                    "장비 내부에 제품이 있어 동일 Recipe 재적용을 차단했습니다. " +
                    "제품을 정상 언로드한 후 다시 적용하십시오. " +
                    "recipe=" + nextRecipeName +
                    ", material=" + materialDetail +
                    (hasPhysicalEvidence ? ", physical=" + physicalDetail : string.Empty);
                return false;
            }

            // 최초 기동 또는 잘못 적용된 Recipe에서 저장 Material의 원래 Recipe로 복구하는 경우만 허용한다.
            if (hasMaterial &&
                !string.IsNullOrWhiteSpace(materialRecipeName) &&
                !string.Equals(currentRecipeName, materialRecipeName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(materialRecipeName, nextRecipeName, StringComparison.OrdinalIgnoreCase))
            {
                materialRecipeRestore = true;
                reason =
                    "장비 내부 Material의 원래 Recipe로 복구합니다. " +
                    "active=" + currentRecipeName +
                    ", materialRecipe=" + materialRecipeName +
                    ", material=" + materialDetail;
                return true;
            }

            if (!hasMaterial && !hasPhysicalEvidence)
                return true;

            reason =
                "장비 내부에 제품이 있어 Recipe 변경을 차단했습니다. " +
                "제품을 정상 언로드한 후 Recipe를 변경하십시오. " +
                "active=" + currentRecipeName +
                ", requested=" + nextRecipeName +
                ", materialRecipe=" + (string.IsNullOrWhiteSpace(materialRecipeName) ? "-" : materialRecipeName) +
                ", material=" + materialDetail +
                (hasPhysicalEvidence ? ", physical=" + physicalDetail : string.Empty);
            return false;
        }

        private void EndRecipeApplyOperation()
        {
            lock (_recipeOperationLock)
            {
                try
                {
                    // 검사/Review 저장 진입 보호를 모두 해제한 뒤 START를 다시 엽니다.
                    try
                    {
                        if (_recipeInspectionResetScope != null)
                            _recipeInspectionResetScope.Dispose();
                    }
                    finally
                    {
                        if (_recipeReviewMaterialResetScope != null)
                            _recipeReviewMaterialResetScope.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _recipeApplyFailureReason = "Recipe 적용 보호 해제 중 오류가 발생하여 START를 차단했습니다. error=" + ex.Message;
                    if (!string.IsNullOrWhiteSpace(_confirmedRecipeSwitchTarget))
                        _failedRecipeSwitchTarget = _confirmedRecipeSwitchTarget;
                    throw;
                }
                finally
                {
                    _recipeInspectionResetScope = null;
                    _recipeReviewMaterialResetScope = null;
                    _confirmedRecipeSwitchPrevious = string.Empty;
                    _confirmedRecipeSwitchTarget = string.Empty;
                    _confirmedRecipeSwitchEmptyMaterial = false;
                    _confirmedRecipeSwitchRequiresHardwareInput = false;
                    _startupMaterialResetOperationActive = false;
                    _recipeApplyOperationActive = false;
                }
            }
        }

        private sealed class RecipeApplyOperationLease : IDisposable
        {
            private MachineController _owner;

            public RecipeApplyOperationLease(MachineController owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                MachineController owner = Interlocked.Exchange(ref _owner, null);
                if (owner != null)
                    owner.EndRecipeApplyOperation();
            }
        }

        // 입력 모드만 예약하는 저장이다. 기존 모션을 막는 전체 Recipe 적용 플래그를 설정하지 않는다.
        internal bool TryBeginInputMapModeSave(string recipeName, out IDisposable lease, out string reason)
        {
            lease = null;
            reason = string.Empty;
            lock (_recipeOperationLock)
            {
                if (_recipeApplyOperationActive || _inputMapModeSaveActive || _recipeStartAttemptCts != null ||
                    !string.Equals(ActiveRecipeName, recipeName, StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(_recipeApplyFailureReason))
                {
                    reason = "레시피 적용/START 준비 상태가 변경되었습니다. 완료 후 입력 맵 사용 모드를 다시 저장하세요.";
                    return false;
                }
                _inputMapModeSaveActive = true;
                lease = new InputMapModeSaveLease(this);
                return true;
            }
        }

        private sealed class InputMapModeSaveLease : IDisposable
        {
            private MachineController _owner;
            internal InputMapModeSaveLease(MachineController owner) { _owner = owner; }
            public void Dispose()
            {
                MachineController owner = System.Threading.Interlocked.Exchange(ref _owner, null);
                if (owner != null) lock (owner._recipeOperationLock) owner._inputMapModeSaveActive = false;
            }
        }

        public void NotifyRecipeConfigurationApplied()
        {
            lock (_recipeOperationLock)
                _recipeConfigurationGeneration++;

            InvalidateRecipeRunReadiness();
        }

        private bool TryBeginRecipeStartAttempt(
            string source,
            out CancellationTokenSource attemptCts,
            out long recipeGeneration,
            out string reason)
        {
            attemptCts = null;
            recipeGeneration = 0;
            reason = string.Empty;

            lock (_recipeOperationLock)
            {
                if (_recipeApplyOperationActive || _inputMapModeSaveActive)
                {
                    reason = "Recipe 저장/적용 작업이 진행 중이므로 START를 시작할 수 없습니다.";
                    return false;
                }

                if (_recipeStartAttemptCts != null)
                {
                    reason = "다른 START 준비 작업이 이미 진행 중입니다.";
                    return false;
                }

                if (_status == EquipmentStatus.Alarm || AlarmManager.HasActive)
                {
                    reason = "Alarm 상태에서는 START를 수행할 수 없습니다.";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(_recipeApplyFailureReason))
                {
                    reason = _recipeApplyFailureReason;
                    return false;
                }

                if (QMC.CDT320.Sequencing.PendingSequenceTaskRegistry.TryGetPending(out reason))
                    return false;

                attemptCts = new CancellationTokenSource();
                _recipeStartAttemptCts = attemptCts;
                recipeGeneration = _recipeConfigurationGeneration;
            }

            QMC.Common.Log.Write(
                "Main",
                "SYSTEM",
                source,
                "START 단일 실행 Gate를 획득했습니다. recipeGeneration=" +
                recipeGeneration + " - Start");
            return true;
        }

        private void EndRecipeStartAttempt(CancellationTokenSource attemptCts, string source)
        {
            if (attemptCts == null)
                return;

            bool released = false;
            lock (_recipeOperationLock)
            {
                if (object.ReferenceEquals(_recipeStartAttemptCts, attemptCts))
                {
                    _recipeStartAttemptCts = null;
                    released = true;
                }
            }

            try { attemptCts.Dispose(); } catch { }
            if (released)
            {
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    source,
                    "START 단일 실행 Gate를 해제했습니다. - End");
            }
        }

        private void CancelRecipeStartAttempt(string reason)
        {
            CancellationTokenSource attemptCts;
            lock (_recipeOperationLock)
                attemptCts = _recipeStartAttemptCts;

            if (attemptCts == null)
                return;

            try
            {
                if (!attemptCts.IsCancellationRequested)
                    attemptCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            QMC.Common.Log.Write(
                "Main",
                "SYSTEM",
                "RecipeStartAttempt",
                "대기 중인 START 준비 작업을 취소했습니다. reason=" +
                (reason ?? string.Empty) + " - Canceled");
        }

        private bool IsRecipeStartAttemptValid(
            CancellationTokenSource attemptCts,
            long recipeGeneration,
            string source,
            out string reason)
        {
            reason = string.Empty;
            if (attemptCts == null || attemptCts.IsCancellationRequested)
            {
                reason = "START 준비 작업이 STOP/Alarm 요청으로 취소되었습니다.";
                return false;
            }

            lock (_recipeOperationLock)
            {
                if (!object.ReferenceEquals(_recipeStartAttemptCts, attemptCts))
                {
                    reason = "현재 START 준비 작업의 소유권이 변경되었습니다.";
                    return false;
                }

                if (_recipeConfigurationGeneration != recipeGeneration)
                {
                    reason =
                        "START 준비 중 Recipe 설정 세대가 변경되었습니다. expected=" +
                        recipeGeneration + ", actual=" + _recipeConfigurationGeneration;
                    return false;
                }
            }

            if (_status == EquipmentStatus.Alarm || AlarmManager.HasActive)
            {
                reason = "START 준비 중 Alarm이 발생했습니다.";
                return false;
            }

            return true;
        }

        public bool CompleteRecipeApplyContext(string recipeName, out string reason)
        {
            reason = string.Empty;
            string normalizedRecipeName = string.IsNullOrWhiteSpace(recipeName)
                ? string.Empty
                : recipeName.Trim();
            if (string.IsNullOrWhiteSpace(normalizedRecipeName) ||
                !string.Equals(
                    ActiveRecipeName ?? string.Empty,
                    normalizedRecipeName,
                    StringComparison.OrdinalIgnoreCase))
            {
                reason =
                    "적용 완료 Recipe와 Controller Active Recipe가 다릅니다. active=" +
                    (ActiveRecipeName ?? string.Empty) +
                    ", applied=" + normalizedRecipeName;
                return false;
            }

            string materialRecipeName;
            string materialDetail;
            if (HasInMachineMaterial(out materialRecipeName, out materialDetail))
            {
                if (string.Equals(
                        materialRecipeName,
                        normalizedRecipeName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                reason =
                    "Recipe 적용 직후 Material이 새로 확인되어 Material Recipe 문맥을 변경할 수 없습니다. " +
                    "active=" + normalizedRecipeName +
                    ", materialRecipe=" + materialRecipeName +
                    ", material=" + materialDetail;
                return false;
            }

            bool hasPhysicalEvidence;
            string physicalDetail;
            string physicalCheckReason;
            if (!TryCollectRecipePhysicalProductEvidence(
                    out hasPhysicalEvidence,
                    out physicalDetail,
                    out physicalCheckReason))
            {
                reason =
                    "Recipe 적용 직후 실제 제품 감지 신호를 확인할 수 없습니다. " +
                    physicalCheckReason;
                return false;
            }

            if (hasPhysicalEvidence)
            {
                reason =
                    "Recipe 적용 직후 미등록 실물이 감지되어 Material Recipe 문맥 변경을 차단했습니다. " +
                    physicalDetail;
                return false;
            }

            MaterialStateService.UpdateRecipeContext(
                normalizedRecipeName,
                "RecipeApplyEmptyMachine");
            return true;
        }

        public bool CompleteMaterialRecipeRestore(string recipeName)
        {
            string restoredRecipeName = string.IsNullOrWhiteSpace(recipeName)
                ? string.Empty
                : recipeName.Trim();
            string materialRecipeName = MaterialStateService.State != null
                ? (MaterialStateService.State.RecipeName ?? string.Empty).Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(restoredRecipeName) ||
                !string.Equals(ActiveRecipeName ?? string.Empty, restoredRecipeName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(materialRecipeName, restoredRecipeName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            lock (_outputFullPreparationLock)
            {
                _outputFullPreparationRequested = false;
                _outputFullPreparationReason = string.Empty;
                _outputFullPreparationRecipeName = restoredRecipeName;
            }

            QMC.Common.Log.Write(
                "Main",
                "SYSTEM",
                "RecipeMaterialRestore",
                "활성 Recipe를 장비 내부 Material Recipe로 복구하고 대기 중인 Output 전체교체 요청을 해제했습니다. " +
                "recipe=" + restoredRecipeName + " - Reset");
            return true;
        }

        public void RequestOutputFullPreparation(string reason, string recipeName)
        {
            lock (_outputFullPreparationLock)
            {
                _outputFullPreparationRequested = true;
                _outputFullPreparationReason = string.IsNullOrWhiteSpace(reason) ? "Requested" : reason;
                _outputFullPreparationRecipeName = string.IsNullOrWhiteSpace(recipeName)
                    ? (ActiveRecipeName ?? string.Empty)
                    : recipeName.Trim();
            }

            QMC.Common.Log.Write("Main", "SYSTEM", "OutputFullPreparation",
                "Output GOOD/NG 전체 준비가 요청되었습니다. reason=" + _outputFullPreparationReason +
                ", recipe=" + _outputFullPreparationRecipeName + " - Set");
        }

        public bool TryGetOutputFullPreparationRequest(out string reason, out string recipeName)
        {
            lock (_outputFullPreparationLock)
            {
                reason = _outputFullPreparationReason ?? string.Empty;
                recipeName = _outputFullPreparationRecipeName ?? string.Empty;
                return _outputFullPreparationRequested;
            }
        }

        public bool CompleteOutputFullPreparation(string recipeName)
        {
            lock (_outputFullPreparationLock)
            {
                string completedRecipeName = recipeName ?? string.Empty;
                if (!string.Equals(ActiveRecipeName ?? string.Empty, completedRecipeName, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(_outputFullPreparationRecipeName) &&
                     !string.Equals(_outputFullPreparationRecipeName, completedRecipeName, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                _outputFullPreparationRequested = false;
                _outputFullPreparationReason = string.Empty;
                _outputFullPreparationRecipeName = completedRecipeName;
            }

            QMC.Common.Log.Write("Main", "SYSTEM", "OutputFullPreparation",
                "Output GOOD/NG 전체 준비가 완료되었습니다. recipe=" + (recipeName ?? string.Empty) + " - Reset");
            return true;
        }

        private bool HasInMachineMaterial(out string materialRecipeName, out string detail)
        {
            var evidence = new List<string>();
            MaterialSnapshot state = MaterialStateService.State;
            materialRecipeName = state != null ? (state.RecipeName ?? string.Empty).Trim() : string.Empty;

            if (state == null)
            {
                detail = "MaterialState=Unavailable";
                return true;
            }

            if (state.Wafers != null)
            {
                foreach (WaferMaterial wafer in state.Wafers)
                {
                    if (wafer == null ||
                        WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
                    {
                        continue;
                    }

                    string location = wafer.CurrentLocation != null
                        ? wafer.CurrentLocation.Kind.ToString()
                        : MaterialLocationKind.Unknown.ToString();
                    evidence.Add("Wafer@" + location + ":" + (wafer.WaferId ?? "-"));
                }
            }

            if (state.Cassettes != null)
            {
                foreach (CassetteMaterial cassette in state.Cassettes)
                {
                    if (cassette == null || cassette.Slots == null)
                        continue;

                    if (cassette.IsEnabled && cassette.IsPresent && !cassette.IsMapped)
                    {
                        evidence.Add(
                            "Cassette@" + cassette.Role +
                            "-L" + cassette.Level +
                            ":Present/Unmapped");
                    }

                    foreach (CassetteSlotMaterial slot in cassette.Slots)
                    {
                        if (slot == null ||
                            (!slot.HasWafer && string.IsNullOrWhiteSpace(slot.WaferId)))
                        {
                            continue;
                        }

                        evidence.Add(
                            "Cassette@" + cassette.Role +
                            "-L" + cassette.Level +
                            "-S" + (slot.SlotNumber + 1) +
                            ":" + (string.IsNullOrWhiteSpace(slot.WaferId) ? "(미등록)" : slot.WaferId) +
                            (slot.HasWafer ? string.Empty : "(HasWafer=False)"));
                    }
                }
            }

            if (state.Dies != null)
            {
                var knownDies = state.Dies
                    .Where(die =>
                        die != null &&
                        die.CurrentLocation != null &&
                        die.CurrentLocation.Kind != MaterialLocationKind.Unknown)
                    .GroupBy(die => die.CurrentLocation.Kind);

                foreach (var group in knownDies)
                {
                    string samples = string.Join(
                        "|",
                        group.Take(3).Select(die => die.DieId ?? "-"));
                    evidence.Add(
                        "Dies@" + group.Key +
                        ":count=" + group.Count() +
                        ", sample=" + samples);
                }
            }

            detail = evidence.Count > 0
                ? string.Join(", ", evidence.Distinct(StringComparer.OrdinalIgnoreCase))
                : "None";
            return evidence.Count > 0;
        }

        private sealed class RecipePresenceSensorState
        {
            public bool Bypassed;
            public bool InputFeeder;
            public bool InputStage;
            public bool OutputStageGood;
            public bool OutputStageNg;
            public bool OutputFeeder;
            public bool InputCassetteProtrusion;
            public bool OutputCassetteProtrusion;
        }

        private bool TryCollectRecipePhysicalProductEvidence(
            out bool hasEvidence,
            out string detail,
            out string reason,
            bool requireHardwareInput = false)
        {
            hasEvidence = false;
            detail = string.Empty;
            reason = string.Empty;

            RecipePresenceSensorState sensors;
            if (!TryReadRecipePresenceSensors(out sensors, out reason, requireHardwareInput))
                return false;

            if (sensors.Bypassed)
                return true;

            var evidence = new List<string>();
            if (sensors.InputFeeder)
                evidence.Add("InputFeederRing=ON");
            if (sensors.InputStage)
                evidence.Add("InputStageRing=ON");
            if (sensors.OutputStageGood)
                evidence.Add("OutputGoodStageRing=ON");
            if (sensors.OutputStageNg)
                evidence.Add("OutputNgStageRing=ON");
            if (sensors.OutputFeeder)
                evidence.Add("OutputFeederRing=ON");
            if (sensors.InputCassetteProtrusion)
                evidence.Add("InputCassetteProtrusion=ON");
            if (sensors.OutputCassetteProtrusion)
                evidence.Add("OutputCassetteProtrusion=ON");

            hasEvidence = evidence.Count > 0;
            detail = hasEvidence ? string.Join(", ", evidence) : "None";
            return true;
        }

        private bool TryValidateRecipeMaterialSensorConsistency(out string reason)
        {
            reason = string.Empty;
            RecipePresenceSensorState sensors;
            if (!TryReadRecipePresenceSensors(out sensors, out reason))
                return false;

            if (sensors.Bypassed)
                return true;

            var mismatches = new List<string>();
            AddRecipePresenceMismatch(
                mismatches,
                MaterialLocationKind.InputFeeder,
                sensors.InputFeeder);
            AddRecipePresenceMismatch(
                mismatches,
                MaterialLocationKind.InputStage,
                sensors.InputStage);
            AddRecipePresenceMismatch(
                mismatches,
                MaterialLocationKind.OutputStageGood,
                sensors.OutputStageGood);
            AddRecipePresenceMismatch(
                mismatches,
                MaterialLocationKind.OutputStageNg,
                sensors.OutputStageNg);
            AddRecipePresenceMismatch(
                mismatches,
                MaterialLocationKind.OutputFeeder,
                sensors.OutputFeeder);

            if (sensors.InputCassetteProtrusion)
                mismatches.Add("InputCassette 돌출 감지=ON");
            if (sensors.OutputCassetteProtrusion)
                mismatches.Add("OutputCassette 돌출 감지=ON");

            if (mismatches.Count == 0)
                return true;

            reason =
                "Material 데이터와 실제 Ring 감지 상태가 일치하지 않습니다. " +
                string.Join(", ", mismatches);
            return false;
        }

        private static void AddRecipePresenceMismatch(
            ICollection<string> mismatches,
            MaterialLocationKind location,
            bool sensorDetected)
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(location);
            bool dataPresent =
                wafer != null &&
                WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty;
            if (dataPresent == sensorDetected)
                return;

            mismatches.Add(
                location +
                "[data=" + (dataPresent ? "Present" : "Empty") +
                ", sensor=" + (sensorDetected ? "ON" : "OFF") + "]");
        }

        private bool TryReadRecipePresenceSensors(
            out RecipePresenceSensorState state,
            out string reason,
            bool requireHardwareInput = false)
        {
            state = new RecipePresenceSensorState();
            reason = string.Empty;

            AppSettings settings = AppSettingsStore.Current;
            bool virtualMode =
                DryRun ||
                GlobalDryRun ||
                (settings != null &&
                 (!settings.UseAjin ||
                  settings.SimulationMode ||
                  settings.DryRunMode ||
                  settings.BypassHardware));
            if (virtualMode && !requireHardwareInput)
            {
                state.Bypassed = true;
                return true;
            }

            if (!AjinFactory.IsRealBoardReady)
            {
                reason =
                    "실장비 I/O 모드이지만 AJIN 보드가 준비되지 않아 제품 감지 센서를 확인할 수 없습니다.";
                return false;
            }

            bool inputFeeder;
            if (!TryReadRecipePresenceSensor(
                    _machine.InputFeederUnit != null
                        ? _machine.InputFeederUnit.WaferFeederRingCheckSensor
                        : null,
                    "InputFeederRing",
                    out inputFeeder,
                    out reason))
            {
                return false;
            }

            bool inputStage8;
            if (!TryReadRecipePresenceSensor(
                    _machine.InputStageUnit != null
                        ? _machine.InputStageUnit.WaferStage8RingCheckSensor
                        : null,
                    "InputStage8Ring",
                    out inputStage8,
                    out reason))
            {
                return false;
            }

            bool inputStage12;
            if (!TryReadRecipePresenceSensor(
                    _machine.InputStageUnit != null
                        ? _machine.InputStageUnit.WaferStage12RingCheckSensor
                        : null,
                    "InputStage12Ring",
                    out inputStage12,
                    out reason))
            {
                return false;
            }

            bool outputStageGood;
            if (!TryReadRecipePresenceSensor(
                    _machine.OutputStageUnit != null
                        ? _machine.OutputStageUnit.GoodBinRingSensor
                        : null,
                    "OutputGoodStageRing",
                    out outputStageGood,
                    out reason))
            {
                return false;
            }

            bool outputStageNg;
            if (!TryReadRecipePresenceSensor(
                    _machine.OutputStageUnit != null
                        ? _machine.OutputStageUnit.NgBinRingSensor
                        : null,
                    "OutputNgStageRing",
                    out outputStageNg,
                    out reason))
            {
                return false;
            }

            bool outputFeeder;
            if (!TryReadRecipePresenceSensor(
                    _machine.OutputFeederUnit != null
                        ? _machine.OutputFeederUnit.BinFeederRingCheckSensor
                        : null,
                    "OutputFeederRing",
                    out outputFeeder,
                    out reason))
            {
                return false;
            }

            bool inputCassetteProtrusion;
            if (!TryReadRecipePresenceSensor(
                    _machine.InputCassetteUnit != null
                        ? _machine.InputCassetteUnit.ProtrusionSensor
                        : null,
                    "InputCassetteProtrusion",
                    out inputCassetteProtrusion,
                    out reason))
            {
                return false;
            }

            bool outputCassetteProtrusion;
            if (!TryReadRecipePresenceSensor(
                    _machine.OutputCassetteUnit != null
                        ? _machine.OutputCassetteUnit.ProtrusionSensor
                        : null,
                    "OutputCassetteProtrusion",
                    out outputCassetteProtrusion,
                    out reason))
            {
                return false;
            }

            state.InputFeeder = inputFeeder;
            state.InputStage = inputStage8 || inputStage12;
            state.OutputStageGood = outputStageGood;
            state.OutputStageNg = outputStageNg;
            state.OutputFeeder = outputFeeder;
            state.InputCassetteProtrusion = inputCassetteProtrusion;
            state.OutputCassetteProtrusion = outputCassetteProtrusion;
            return true;
        }

        private static bool TryReadRecipePresenceSensor(
            BaseDigitalInput sensor,
            string sensorName,
            out bool detected,
            out string reason)
        {
            detected = false;
            reason = string.Empty;
            if (sensor == null)
            {
                reason = sensorName + " 센서 객체가 없습니다.";
                return false;
            }

            int errorCode;
            if (!AjinIoScanService.TryReadHardwareInput(sensor, out errorCode))
            {
                reason =
                    sensorName + " 실입력 읽기에 실패했습니다. errorCode=" + errorCode;
                return false;
            }

            detected = sensor.IsOn;
            return true;
        }

    }
}
