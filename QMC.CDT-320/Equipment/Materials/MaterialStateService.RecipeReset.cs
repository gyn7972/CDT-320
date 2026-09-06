using System;
using System.Linq;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Sequencing;
using QMC.Common;

namespace QMC.CDT320.Materials
{
    public static partial class MaterialStateService
    {
        private static readonly object _inputStageReviewRecipeResetSync = new object();
        private static int _inputStageReviewMaterialOperationCount;
        private static bool _inputStageReviewRecipeResetActive;

        /// <summary>
        /// Controller의 작업 종료 표식과 별개로 실제 Review 확정/저장 함수가 반환했는지 확인합니다.
        /// 이 잠금 안에서는 Material/Controller 잠금, 저장 IO 또는 Task 대기를 수행하지 않습니다.
        /// </summary>
        internal static bool TryBeginInputStageReviewRecipeReset(out IDisposable scope, out string reason)
        {
            scope = null;
            lock (_inputStageReviewRecipeResetSync)
            {
                if (_inputStageReviewRecipeResetActive || _inputStageReviewMaterialOperationCount != 0)
                {
                    reason = "Review 확정/기준 저장 또는 Material 초기화가 아직 끝나지 않아 Recipe 적용을 차단했습니다.";
                    return false;
                }
                _inputStageReviewRecipeResetActive = true;
                scope = new InputStageReviewMaterialOperationScope(true);
                reason = string.Empty;
                return true;
            }
        }

        private static bool TryBeginInputStageReviewMaterialOperation(out IDisposable scope, out string reason)
        {
            scope = null;
            lock (_inputStageReviewRecipeResetSync)
            {
                if (_inputStageReviewRecipeResetActive || _inputStageReviewMaterialOperationCount == int.MaxValue)
                {
                    reason = "Recipe 적용 및 Material 초기화 중에는 Review 확정/기준 저장을 시작할 수 없습니다.";
                    return false;
                }
                _inputStageReviewMaterialOperationCount++;
                scope = new InputStageReviewMaterialOperationScope(false);
                reason = string.Empty;
                return true;
            }
        }

        public static bool CommitInputStageRunReview(WaferMaterial wafer, UserConfirmResult review, out string reason)
        {
            IDisposable scope;
            if (!TryBeginInputStageReviewMaterialOperation(out scope, out reason))
                return false;
            using (scope)
                return CommitInputStageRunReviewCore(wafer, review, out reason);
        }

        public static bool TryApproveInputStageRunReviewWithDefaultOrder(WaferMaterial wafer, out string reason)
        {
            IDisposable scope;
            if (!TryBeginInputStageReviewMaterialOperation(out scope, out reason))
                return false;
            using (scope)
                return TryApproveInputStageRunReviewWithDefaultOrderCore(wafer, out reason);
        }

        public static bool TryEstablishInputStageReviewBaseline(
            string waferId, DieMap draft, long sessionGeneration, long requestGeneration, out string reason)
        {
            IDisposable scope;
            if (!TryBeginInputStageReviewMaterialOperation(out scope, out reason))
                return false;
            using (scope)
                return TryEstablishInputStageReviewBaselineCore(waferId, draft, sessionGeneration, requestGeneration, out reason);
        }

        private sealed class InputStageReviewMaterialOperationScope : IDisposable
        {
            private readonly bool _isReset;
            private bool _disposed;

            internal InputStageReviewMaterialOperationScope(bool isReset) { _isReset = isReset; }

            public void Dispose()
            {
                lock (_inputStageReviewRecipeResetSync)
                {
                    if (_disposed)
                        return;
                    _disposed = true;
                    if (_isReset)
                        _inputStageReviewRecipeResetActive = false;
                    else
                        _inputStageReviewMaterialOperationCount--;
                }
            }
        }

        /// <summary>작업자 확인 전 Material 참조와 변경 세대를 같은 잠금에서 캡처합니다.</summary>
        internal static void CaptureRecipeResetState(out MaterialSnapshot expectedState, out long expectedVersion)
        {
            lock (_stateSync)
            {
                expectedState = State;
                expectedVersion = _stateVersion;
            }
        }

        internal static bool IsRecipeResetStateCurrent(MaterialSnapshot expectedState, long expectedVersion)
        {
            string reason;
            return IsRecipeResetStateCurrent(expectedState, expectedVersion, out reason);
        }

        internal static bool IsRecipeResetStateCurrent(
            MaterialSnapshot expectedState, long expectedVersion, out string reason)
        {
            lock (_stateSync)
                return CheckRecipeResetStateNoLock(expectedState, expectedVersion, out reason);
        }

        /// <summary>
        /// 운전·실물·Review 종료 확인과 레시피 적용 보호는 호출자가 유지합니다.
        /// 성공은 메모리 교체와 저장 요청 등록까지이며, 호출자가 잠금 밖에서 TryFlushPendingSave를 확인해야 합니다.
        /// 기동 시 레시피가 없으면 기본 후보의 빈 RecipeName도 허용합니다. A→B 대상 이름은 Factory에서 검증합니다.
        /// </summary>
        internal static bool TryReplaceRecipeState(
            MaterialSnapshot expectedState,
            long expectedVersion,
            MaterialSnapshot candidate,
            string reason,
            out string error)
        {
            error = "";
            string saveReason = string.IsNullOrWhiteSpace(reason) ? "InitializeRecipeMaterialReset" : reason;
            bool replaced = false;
            try
            {
                lock (_stateSync)
                {
                    if (!CheckInputStageReviewRecipeResetAdmission(out error) ||
                        !CheckRecipeResetStateNoLock(expectedState, expectedVersion, out error) ||
                        !CheckEmptyRecipeResetCandidateNoLock(candidate, out error))
                        return false;

                    // 검증 실패 시 기존 State를 유지하는 공식 교체 경로를 사용합니다.
                    MaterialStorage.ReplaceState(candidate);
                    replaced = true;
                    bool notified = false;
                    try
                    {
                        InvalidateDieByIdIndexNoLock();
                        _outputReceiveOrderCache.Clear();
                        lock (_outputReceiveCapacitySync)
                            _outputReceiveCapacityCache.Clear();
                        InputStageHybridResultSession.Clear();
                        ClearCompletedInputStageReviewForRecipeResetNoLock();
                        ClearAllPickerFlowRecoveries("레시피 변경: " + candidate.RecipeName);
                    }
                    finally
                    {
                        // 교체 이후 정리에 실패하더라도 새 상태의 자료 세대/저장 요청은 잃지 않습니다.
                        // 기존 저장 Revision과 내구성 워터마크를 초기화하면 늦은 이전 저장이 새 상태를 덮을 수 있습니다.
                        notified = TryNotifyAndSave(saveReason);
                    }
                    if (!notified)
                    {
                        error = "새 레시피 Material로 교체했으나 저장 요청을 등록하지 못했습니다. 운전을 시작할 수 없습니다.";
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = (replaced ? "새 레시피 Material 교체 후 정리 실패" : "새 레시피 Material 교체 실패") +
                    ". 운전을 시작할 수 없습니다. " + ex.Message;
                Log.Write(LogLevel.AboveNormal, "Main", "RecipeMaterialReset", error + " - Failed");
                return false;
            }
        }

        private static bool CheckInputStageReviewRecipeResetAdmission(out string reason)
        {
            lock (_inputStageReviewRecipeResetSync)
            {
                if (!_inputStageReviewRecipeResetActive || _inputStageReviewMaterialOperationCount != 0)
                {
                    reason = "Review 저장 종료 및 Recipe 초기화 보호가 확인되지 않아 Material을 교체하지 않았습니다.";
                    return false;
                }
                reason = string.Empty;
                return true;
            }
        }

        private static void ClearCompletedInputStageReviewForRecipeResetNoLock()
        {
            // 실제 작업 종료는 admission counter로 확인합니다. 실패 후 남은 pending은 실행 중 표식이 아닙니다.
            InvalidateInputStageReviewVerification(0, 0);
            // 이미 세대가 0인 종료 경로에서도 이전 승인 서명이 남지 않도록 함께 지웁니다.
            _inputStageReviewAcceptedSourceSignature = null;
            _inputStageReviewAcceptedCandidateSignature = null;
            _inputStageReviewAcceptedConditionSignature = null;
            _inputStageReviewLastRejectionKey = null;
            _inputStageReviewBaselineSavePendingWafer = null;
            _inputStageReviewPendingSave.Clear();
        }

        private static bool CheckRecipeResetStateNoLock(
            MaterialSnapshot expectedState, long expectedVersion, out string reason)
        {
            reason = "";
            if (expectedState == null || !ReferenceEquals(State, expectedState) || _stateVersion != expectedVersion)
            {
                reason = "레시피 변경 확인 중 Material이 변경되었습니다. 현재 상태를 다시 확인하십시오.";
                return false;
            }
            if (_stateVersion == long.MaxValue)
            {
                reason = "Material 변경 세대를 더 증가시킬 수 없습니다. 레시피 변경을 중단합니다.";
                return false;
            }
            return true;
        }

        private static bool CheckEmptyRecipeResetCandidateNoLock(MaterialSnapshot candidate, out string reason)
        {
            reason = "";
            if (candidate == null || ReferenceEquals(candidate, State) ||
                candidate.Cassettes == null || candidate.Wafers == null || candidate.Dies == null ||
                candidate.Wafers.Count != 0 || candidate.Dies.Count != 0 ||
                !string.Equals(candidate.PickupBinMode, PickupBinModeAll, StringComparison.Ordinal) ||
                candidate.PickupBinNumbers == null || candidate.PickupBinNumbers.Count != 0)
            {
                reason = "새 레시피의 빈 Material 후보가 올바르지 않습니다.";
                return false;
            }
            if (ReferenceEquals(candidate.Cassettes, State.Cassettes) ||
                ReferenceEquals(candidate.Wafers, State.Wafers) || ReferenceEquals(candidate.Dies, State.Dies) ||
                candidate.Cassettes.Any(cassette => cassette == null || cassette.IsPresent || cassette.IsMapped ||
                    !string.IsNullOrWhiteSpace(cassette.CassetteLotId) || cassette.Slots == null ||
                    cassette.Slots.Any(slot => slot == null || slot.HasWafer ||
                        !string.IsNullOrWhiteSpace(slot.WaferId) || !string.IsNullOrWhiteSpace(slot.WaferInstanceId)) ||
                    (State.Cassettes != null && State.Cassettes.Any(previous => ReferenceEquals(previous, cassette) ||
                        (previous != null && ReferenceEquals(previous.Slots, cassette.Slots))))))
            {
                reason = "새 Material 후보에 기존 카세트 참조 또는 잔여 정보가 포함되어 있습니다.";
                return false;
            }
            return true;
        }
    }
}
