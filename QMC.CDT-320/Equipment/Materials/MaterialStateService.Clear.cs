using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;

namespace QMC.CDT320.Materials
{
    // MaterialStateService partial: Stage/Cassette 자재 Clear 및 대상 수집 (원본 2797-4449)
    public static partial class MaterialStateService
    {
        /// <summary>
        /// 자재 Clear 차단 사유 기록 — 반드시 디스크에 남아야 한다.
        /// [사용자 확정 2026-08-17] 기존에는 4-인자 Log.Write를 썼는데, 이 형식은
        ///   LogPolicy.IsDiagnosticVerbose가 꺼져 있으면 Log.cs에서 통째로 버려진다.
        ///   그 결과 화면은 "로그를 확인하십시오"라고 안내하는데 정작 사유 로그가 남지 않아
        ///   원인을 추적할 수 없었다(2026-08-08 스냅샷 저장 실패 때와 동일한 결함).
        /// 현재 기준: EventKind.Warning으로 올려 최소 로그 정책에서도 보존한다.
        ///   호출자는 동일 사유 문자열을 화면 메시지로도 사용한다.
        /// </summary>
        private static void LogMaterialClearBlocked(string scope, string reason)
        {
            string message = (scope ?? "Material clear") + " blocked: " + (reason ?? "");
            QMC.Common.Logging.EventLogger.Write(
                QMC.Common.Logging.EventKind.Warning,
                "SYSTEM",
                "MATERIAL-CLEAR-BLOCKED",
                message);
        }

        public static bool ClearWaferAtLocation(MaterialLocationKind kind)
        {
            if (kind == MaterialLocationKind.InputStage ||
                kind == MaterialLocationKind.OutputStageGood ||
                kind == MaterialLocationKind.OutputStageNg)
            {
                return ClearStageMaterialData(kind);
            }

            MaterialCompactionResult compactionResult;
            int removedDieCount;
            lock (_stateSync)
            {
                var wafers = State.Wafers
                    .Where(w => w.CurrentLocation != null &&
                                w.CurrentLocation.Kind == kind)
                    .ToList();
                if (wafers.Count == 0)
                    return false;

                bool outputSide = kind == MaterialLocationKind.OutputFeeder ||
                                  kind == MaterialLocationKind.OutputCassette;
                bool inputFeeder = kind == MaterialLocationKind.InputFeeder;
                List<DieMaterial> outputDies = null;
                List<DieMaterial> inputDies = null;
                List<DieMaterial> inputHistoryDies = null;
                bool preserveInputHistory = false;
                if (outputSide)
                {
                    string outputClearReason;
                    bool canClear = kind == MaterialLocationKind.OutputCassette
                        ? TryCollectOutputCassetteClearDiesNoLock(
                            wafers,
                            out outputDies,
                            out outputClearReason)
                        : TryCollectOutputLocationClearDiesNoLock(
                            wafers,
                            kind,
                            out outputDies,
                            out outputClearReason);
                    if (!canClear)
                    {
                        LogMaterialClearBlocked("위치 Clear(참조 무결성)", "location=" + kind + ", detail=" + outputClearReason);
                        return false;
                    }
                }
                else if (inputFeeder)
                {
                    string inputClearReason;
                    if (!TryCollectInputLocationClearDiesNoLock(
                        wafers,
                        kind,
                        out inputDies,
                        out inputClearReason))
                    {
                        LogMaterialClearBlocked("위치 Clear(참조 무결성)", "location=" + kind + ", detail=" + inputClearReason);
                        return false;
                    }
                    if (!TryCollectInputHistoryForClearNoLock(
                        wafers,
                        out inputHistoryDies,
                        out preserveInputHistory,
                        out inputClearReason))
                    {
                        LogMaterialClearBlocked("위치 Clear(history 확인 실패)", "location=" + kind + ", detail=" + inputClearReason);
                        return false;
                    }
                }

                if (outputSide)
                {
                    removedDieCount = DetachOrRemoveClearedOutputDiesNoLock(outputDies);
                }
                else if (inputFeeder && preserveInputHistory)
                {
                    DemotePreservedInputHistoryLocationsNoLock(inputHistoryDies, kind);
                    removedDieCount = 0;
                }
                else
                {
                    removedDieCount = inputFeeder
                        ? RemoveDieMaterialsNoLock(inputDies)
                        : RemoveDiesAtClearedLocationNoLock(wafers, kind);
                }

                foreach (var wafer in wafers)
                {
                    if (outputSide)
                        ClearOutputStageWaferProcessingFieldsNoLock(wafer);
                    else if (inputFeeder && !preserveInputHistory)
                        ClearInputStageWaferProcessingFieldsNoLock(wafer);
                    wafer.State = WaferMaterialState.Empty;
                    wafer.CurrentLocation = MaterialLocation.Unknown();
                    wafer.UpdatedAt = DateTime.Now;
                }
                compactionResult = CompactMaterialStateNoLock();
            }

            LogMaterialCompaction("ClearWaferAtLocation:" + kind, compactionResult);
            if (removedDieCount > 0)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "위치 Clear와 연결된 Die Material을 정리했습니다. location=" + kind +
                    ", removedDies=" + removedDieCount + " - Ok");
            }
            NotifyAndSave("ClearWaferAtLocation");
            return true;
        }

        /// <summary>
        /// Stage DATA CLEAR는 화면의 Wafer 한 건만 비우지 않고 해당 Stage에 연결된
        /// Die 위치, 공정 Map/수령 계획, 예약 상태까지 함께 초기화한다.
        /// 다른 Stage 또는 Picker에 실제로 이동한 Die는 수동 Clear 범위에서 제외한다.
        /// </summary>
        private static bool ClearStageMaterialData(MaterialLocationKind kind)
        {
            int clearedWaferCount = 0;
            int removedDieCount = 0;
            MaterialCompactionResult compactionResult = null;

            lock (_stateSync)
            {
                List<WaferMaterial> affectedWafers;
                string affectedWaferReason;
                if (!TryCollectStageClearAffectedWafersNoLock(
                    kind,
                    out affectedWafers,
                    out affectedWaferReason))
                {
                    LogMaterialClearBlocked("Stage Clear(대상 확인 실패)", "location=" + kind + ", detail=" + affectedWaferReason);
                    return false;
                }

                List<DieMaterial> clearDies;
                string clearReason;
                bool canClear = kind == MaterialLocationKind.InputStage
                    ? TryCollectInputLocationClearDiesNoLock(
                        affectedWafers,
                        kind,
                        out clearDies,
                        out clearReason)
                    : TryCollectOutputLocationClearDiesNoLock(
                        affectedWafers,
                        kind,
                        out clearDies,
                        out clearReason);
                if (!canClear)
                {
                    LogMaterialClearBlocked("Stage Clear(참조 무결성)", "location=" + kind + ", detail=" + clearReason);
                    return false;
                }

                List<DieMaterial> inputHistoryDies = null;
                bool preserveInputHistory = false;
                if (kind == MaterialLocationKind.InputStage &&
                    !TryCollectInputHistoryForClearNoLock(
                        affectedWafers,
                        out inputHistoryDies,
                        out preserveInputHistory,
                        out clearReason))
                {
                    LogMaterialClearBlocked("Stage Clear(history 확인 실패)", "location=" + kind + ", detail=" + clearReason);
                    return false;
                }

                if (kind == MaterialLocationKind.InputStage && preserveInputHistory)
                {
                    DemotePreservedInputHistoryLocationsNoLock(inputHistoryDies, kind);
                    removedDieCount = 0;
                }
                else
                {
                    removedDieCount = kind == MaterialLocationKind.InputStage
                        ? RemoveDieMaterialsNoLock(clearDies)
                        : DetachOrRemoveClearedOutputDiesNoLock(clearDies);
                }

                foreach (WaferMaterial wafer in affectedWafers)
                {
                    if (kind == MaterialLocationKind.InputStage)
                    {
                        if (!preserveInputHistory)
                        {
                            ClearInputStageWaferProcessingFieldsNoLock(wafer);
                            wafer.InputStageProcessingGeneration = wafer.InputStageProcessingGeneration + 1;
                        }
                        // Picker/Output에 이미 이동한 Die나 Output source pointer가 이 physical wafer를
                        // 계속 참조할 수 있으므로 Stage Clear 시 identity를 회전시키지 않는다.
                        // 새 wafer Mapping의 비보존 경계에서만 중앙 reset helper가 새 identity를 발급한다.
                    }
                    else
                    {
                        ClearOutputStageWaferProcessingFieldsNoLock(wafer);
                    }

                    wafer.State = WaferMaterialState.Empty;
                    wafer.CurrentLocation = MaterialLocation.Unknown();
                    wafer.UpdatedAt = DateTime.Now;
                    clearedWaferCount++;
                }

                if (kind == MaterialLocationKind.InputStage)
                {
                    InputStageHybridResultSession.Clear();
                }
                else
                {
                    QMC.CDT320.BinSide side = kind == MaterialLocationKind.OutputStageNg
                        ? QMC.CDT320.BinSide.Ng
                        : QMC.CDT320.BinSide.Good;
                    _outputReceiveOrderCache.Remove(side);
                }

                if (clearedWaferCount > 0 || removedDieCount > 0)
                    compactionResult = CompactMaterialStateNoLock();
            }

            if (clearedWaferCount == 0 && removedDieCount == 0)
                return false;

            string saveReason = "ClearStageMaterialData:" + kind;
            LogMaterialCompaction(saveReason, compactionResult);
            NotifyAndSave(saveReason);
            Log.Write(
                "Main",
                "SYSTEM",
                "MaterialStateService",
                "Stage Material 데이터를 전체 초기화했습니다. location=" + kind +
                ", wafers=" + clearedWaferCount +
                ", removedDies=" + removedDieCount + " - Ok");
            return true;
        }

        private static bool TryCollectStageClearAffectedWafersNoLock(
            MaterialLocationKind kind,
            out List<WaferMaterial> affectedWafers,
            out string reason)
        {
            affectedWafers = new List<WaferMaterial>();
            reason = "";
            if (State == null || State.Wafers == null || State.Dies == null)
                return true;

            var affected = new HashSet<WaferMaterial>(State.Wafers.Where(wafer =>
                wafer != null &&
                wafer.CurrentLocation != null &&
                wafer.CurrentLocation.Kind == kind));

            // Stage→Feeder 이송은 Wafer 위치를 먼저 바꾸고 Die의 Stage 태그를 유지할 수 있다.
            // 따라서 Stage-tagged Die의 parent를 무조건 Clear 대상으로 역추적하지 않는다.
            // 구버전에서 Wafer만 Unknown이 된 orphan parent만 복구 대상으로 포함한다.
            foreach (DieMaterial die in State.Dies)
            {
                if (die == null ||
                    die.CurrentLocation == null ||
                    die.CurrentLocation.Kind != kind)
                {
                    continue;
                }

                string parentInstanceId = kind == MaterialLocationKind.InputStage
                    ? (die.InputWaferInstanceId ?? "").Trim()
                    : (die.OutputWaferInstanceId ?? "").Trim();
                string parentDisplayId = kind == MaterialLocationKind.InputStage
                    ? (die.WaferID_Input ?? "").Trim()
                    : (die.WaferID_Output ?? "").Trim();

                List<WaferMaterial> candidates;
                if (!string.IsNullOrWhiteSpace(parentInstanceId))
                {
                    candidates = State.Wafers.Where(wafer =>
                            wafer != null &&
                            string.Equals(
                                (wafer.WaferInstanceId ?? "").Trim(),
                                parentInstanceId,
                                StringComparison.OrdinalIgnoreCase))
                        .Take(2)
                        .ToList();
                }
                else if (!string.IsNullOrWhiteSpace(parentDisplayId))
                {
                    candidates = State.Wafers.Where(wafer =>
                            wafer != null &&
                            string.Equals(
                                (wafer.WaferId ?? "").Trim(),
                                parentDisplayId,
                                StringComparison.OrdinalIgnoreCase))
                        .Take(2)
                        .ToList();
                }
                else
                {
                    continue;
                }

                if (candidates.Count > 1)
                {
                    reason = "Stage-tagged Die parent를 하나로 확인할 수 없습니다. die=" +
                             (die.DieId ?? "") +
                             ", instance=" + parentInstanceId +
                             ", wafer=" + parentDisplayId;
                    affectedWafers.Clear();
                    return false;
                }
                if (candidates.Count == 0)
                    continue;

                WaferMaterial parent = candidates[0];
                if (!string.IsNullOrWhiteSpace(parentInstanceId) &&
                    !string.IsNullOrWhiteSpace(parentDisplayId) &&
                    !string.Equals(
                        parent.WaferId ?? "",
                        parentDisplayId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    reason = "Stage-tagged Die의 instance/display parent가 일치하지 않습니다. die=" +
                             (die.DieId ?? "") +
                             ", instance=" + parentInstanceId +
                             ", pointerWafer=" + parentDisplayId +
                             ", actualWafer=" + (parent.WaferId ?? "");
                    affectedWafers.Clear();
                    return false;
                }

                MaterialLocationKind parentLocation = parent.CurrentLocation != null
                    ? parent.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                if (parentLocation == MaterialLocationKind.Unknown)
                    affected.Add(parent);
            }

            affectedWafers = affected.ToList();
            return true;
        }

        private static void ClearOutputStageWaferProcessingFieldsNoLock(WaferMaterial wafer)
        {
            if (wafer == null)
                return;

            wafer.OutputResultFileSessionStartedAt = null;
            wafer.OutputReceiveSourceWaferId = string.Empty;
            wafer.OutputReceiveSourceWaferInstanceId = string.Empty;
            wafer.OutputReceiveDieMapX = 0;
            wafer.OutputReceiveDieMapY = 0;
            wafer.OutputReceivePitchX = 0.0;
            wafer.OutputReceivePitchY = 0.0;
            wafer.OutputReceiveDieSizeX = 0.0;
            wafer.OutputReceiveDieSizeY = 0.0;
            wafer.OutputReceiveOuterDiameterMm = 0.0;
            wafer.OutputReceiveOriginX = 0.0;
            wafer.OutputReceiveOriginY = 0.0;
            wafer.OutputReceiveNextIndex = 0;
            wafer.OutputReceiveTotalCount = 0;
            wafer.OutputReceiveTargetCount = 0;
            wafer.OutputReceiveStartCorner = string.Empty;
            wafer.OutputReceiveDirection = string.Empty;
            wafer.OutputReceivePattern = string.Empty;
            wafer.OutputReceiveSlots = new List<OutputReceiveSlotMaterial>();
            wafer.DieMapFrameObjId = string.Empty;
            wafer.OutputGrade = DieResult.Unknown;
        }

        private static int RemoveDiesAtClearedLocationNoLock(
            ICollection<WaferMaterial> wafers,
            MaterialLocationKind locationKind)
        {
            if (wafers == null || wafers.Count == 0 || State.Dies == null)
                return 0;

            bool inputSide = locationKind == MaterialLocationKind.InputCassette ||
                             locationKind == MaterialLocationKind.InputFeeder ||
                             locationKind == MaterialLocationKind.InputStage;
            var waferInstanceIds = new HashSet<string>(
                wafers
                    .Where(w => w != null && !string.IsNullOrWhiteSpace(w.WaferInstanceId))
                    .Select(w => w.WaferInstanceId.Trim()),
                StringComparer.OrdinalIgnoreCase);
            var waferDisplayIds = new HashSet<string>(
                wafers
                    .Where(w => w != null && !string.IsNullOrWhiteSpace(w.WaferId))
                    .Select(w => w.WaferId.Trim()),
                StringComparer.OrdinalIgnoreCase);
            var uniqueDisplayIds = new HashSet<string>(
                State.Wafers
                    .Where(w => w != null && !string.IsNullOrWhiteSpace(w.WaferId))
                    .GroupBy(w => w.WaferId.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() == 1)
                    .Select(group => group.Key),
                StringComparer.OrdinalIgnoreCase);
            var removeDies = new HashSet<DieMaterial>();

            foreach (DieMaterial die in State.Dies)
            {
                if (die == null)
                    continue;

                MaterialLocationKind dieLocation = die.CurrentLocation != null
                    ? die.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                bool locationMatches = dieLocation == locationKind ||
                                       (locationKind == MaterialLocationKind.InputStage &&
                                        dieLocation == MaterialLocationKind.Unknown);
                if (!locationMatches)
                    continue;

                string parentInstanceId = inputSide
                    ? (die.InputWaferInstanceId ?? "").Trim()
                    : (die.OutputWaferInstanceId ?? "").Trim();
                string parentDisplayId = inputSide
                    ? (die.WaferID_Input ?? "").Trim()
                    : (die.WaferID_Output ?? "").Trim();
                bool related = !string.IsNullOrWhiteSpace(parentInstanceId)
                    ? waferInstanceIds.Contains(parentInstanceId)
                    : !string.IsNullOrWhiteSpace(parentDisplayId) &&
                      uniqueDisplayIds.Contains(parentDisplayId) &&
                      waferDisplayIds.Contains(parentDisplayId);
                if (related)
                    removeDies.Add(die);
            }

            if (removeDies.Count == 0)
                return 0;

            return RemoveDieMaterialsNoLock(removeDies);
        }

        private static int RemoveDieMaterialsNoLock(ICollection<DieMaterial> removeDies)
        {
            if (removeDies == null || removeDies.Count == 0 || State.Dies == null)
                return 0;

            var removeSet = new HashSet<DieMaterial>(removeDies.Where(d => d != null));
            if (removeSet.Count == 0)
                return 0;

            int removed = State.Dies.RemoveAll(die => die != null && removeSet.Contains(die));
            if (removed > 0)
                InvalidateDieByIdIndexNoLock();
            var remainingDieIds = new HashSet<string>(
                State.Dies
                    .Where(d => d != null && !string.IsNullOrWhiteSpace(d.DieId))
                    .Select(d => d.DieId.Trim()),
                StringComparer.OrdinalIgnoreCase);
            var removedDieIds = new HashSet<string>(
                removeSet
                    .Where(d => d != null && !string.IsNullOrWhiteSpace(d.DieId))
                    .Select(d => d.DieId.Trim())
                    .Where(id => !remainingDieIds.Contains(id)),
                StringComparer.OrdinalIgnoreCase);
            if (removedDieIds.Count > 0)
            {
                foreach (WaferMaterial wafer in State.Wafers)
                {
                    if (wafer != null && wafer.DieIds != null)
                    {
                        wafer.DieIds.RemoveAll(id =>
                            !string.IsNullOrWhiteSpace(id) && removedDieIds.Contains(id.Trim()));
                    }
                }
            }

            return removed;
        }

        private static int DetachOrRemoveClearedOutputDiesNoLock(
            ICollection<DieMaterial> outputDies)
        {
            List<DieMaterial> preserveDies;
            List<DieMaterial> removeDies;
            string reason;
            if (!TryClassifyOutputDiesForDetachNoLock(
                outputDies,
                out preserveDies,
                out removeDies,
                out reason))
            {
                throw new InvalidOperationException(reason);
            }

            foreach (DieMaterial die in preserveDies)
            {
                // Source Input wafer가 아직 작업 집합이면 Review/진행 복구에 Die 자체가 필요하다.
                // Output cassette parent만 끊어 cleared output wafer가 graph에 남지 않게 한다.
                die.WaferID_Output = "";
                die.OutputWaferInstanceId = "";
                die.Output_BinCode = 0;
                die.Bin_IndexX = -1;
                die.Bin_IndexY = -1;
                die.BinOffset = new VisionOffset();
                die.CurrentLocation = MaterialLocation.Unknown();
                die.UpdatedAt = DateTime.Now;
            }

            return RemoveDieMaterialsNoLock(removeDies);
        }

        private static bool TryClassifyOutputDiesForDetachNoLock(
            ICollection<DieMaterial> outputDies,
            out List<DieMaterial> preserveDies,
            out List<DieMaterial> removeDies,
            out string reason)
        {
            preserveDies = new List<DieMaterial>();
            removeDies = new List<DieMaterial>();
            reason = "";
            if (outputDies == null || outputDies.Count == 0)
                return true;

            foreach (DieMaterial die in outputDies.Where(d => d != null).Distinct())
            {
                string inputInstance = (die.InputWaferInstanceId ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(inputInstance))
                {
                    List<WaferMaterial> inputCandidates = State.Wafers.Where(w =>
                            w != null &&
                            string.Equals(
                                (w.WaferInstanceId ?? "").Trim(),
                                inputInstance,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    int candidateCount = inputCandidates.Count;
                    if (candidateCount != 1)
                    {
                        reason = "Output clear Die의 Input parent를 하나로 확인할 수 없습니다. die=" +
                                 (die.DieId ?? "") +
                                 ", instance=" + inputInstance +
                                 ", candidates=" + candidateCount;
                        preserveDies.Clear();
                        removeDies.Clear();
                        return false;
                    }
                    if (!string.IsNullOrWhiteSpace(die.WaferID_Input) &&
                        !string.Equals(
                            inputCandidates[0].WaferId ?? "",
                            die.WaferID_Input,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        reason = "Output clear Die의 Input instance/display parent가 일치하지 않습니다. die=" +
                                 (die.DieId ?? "") +
                                 ", instance=" + inputInstance +
                                 ", pointerWafer=" + (die.WaferID_Input ?? "") +
                                 ", actualWafer=" + (inputCandidates[0].WaferId ?? "");
                        preserveDies.Clear();
                        removeDies.Clear();
                        return false;
                    }
                    preserveDies.Add(die);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(die.WaferID_Input))
                {
                    int candidateCount = State.Wafers.Count(w =>
                            w != null &&
                            string.Equals(
                                w.WaferId ?? "",
                                die.WaferID_Input,
                                StringComparison.OrdinalIgnoreCase));
                    if (candidateCount > 1)
                    {
                        reason = "Output clear Die의 legacy Input parent가 중복되었습니다. die=" +
                                 (die.DieId ?? "") +
                                 ", wafer=" + (die.WaferID_Input ?? "") +
                                 ", candidates=" + candidateCount;
                        preserveDies.Clear();
                        removeDies.Clear();
                        return false;
                    }
                    if (candidateCount == 1)
                    {
                        preserveDies.Add(die);
                        continue;
                    }
                }

                removeDies.Add(die);
            }
            return true;
        }

        private static bool TryCollectOutputParentDiesNoLock(
            ICollection<WaferMaterial> targetWafers,
            out List<DieMaterial> relatedDies,
            out string reason)
        {
            relatedDies = new List<DieMaterial>();
            reason = "";
            if (targetWafers == null || State == null || State.Wafers == null || State.Dies == null)
                return true;

            List<WaferMaterial> targets = targetWafers.Where(wafer => wafer != null).Distinct().ToList();
            if (targets.Count == 0)
                return true;

            var targetSet = new HashSet<WaferMaterial>(targets);
            var targetInstanceIds = new HashSet<string>(
                targets
                    .Select(EnsureWaferInstanceIdNoLock)
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            var targetDisplayIds = new HashSet<string>(
                targets
                    .Select(wafer => (wafer.WaferId ?? "").Trim())
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);

            foreach (string targetInstanceId in targetInstanceIds)
            {
                int candidateCount = State.Wafers.Count(wafer =>
                    wafer != null &&
                    string.Equals(
                        (wafer.WaferInstanceId ?? "").Trim(),
                        targetInstanceId,
                        StringComparison.OrdinalIgnoreCase));
                if (candidateCount != 1)
                {
                    reason = "Output parent instance를 하나로 확인할 수 없습니다. instance=" +
                             targetInstanceId + ", candidates=" + candidateCount;
                    return false;
                }
            }

            foreach (DieMaterial die in State.Dies)
            {
                if (die == null)
                    continue;

                string outputInstance = (die.OutputWaferInstanceId ?? "").Trim();
                string outputDisplay = (die.WaferID_Output ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(outputInstance))
                {
                    if (!targetInstanceIds.Contains(outputInstance))
                        continue;

                    WaferMaterial parent = targets.FirstOrDefault(wafer =>
                        string.Equals(
                            (wafer.WaferInstanceId ?? "").Trim(),
                            outputInstance,
                            StringComparison.OrdinalIgnoreCase));
                    if (parent == null ||
                        (!string.IsNullOrWhiteSpace(outputDisplay) &&
                         !string.Equals(
                             outputDisplay,
                             parent.WaferId ?? "",
                             StringComparison.OrdinalIgnoreCase)))
                    {
                        reason = "Die의 Output instance/display parent가 일치하지 않습니다. die=" +
                                 (die.DieId ?? "") + ", instance=" + outputInstance +
                                 ", display=" + outputDisplay;
                        relatedDies.Clear();
                        return false;
                    }

                    relatedDies.Add(die);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(outputDisplay) || !targetDisplayIds.Contains(outputDisplay))
                    continue;

                List<WaferMaterial> displayCandidates = State.Wafers.Where(wafer =>
                        wafer != null &&
                        string.Equals(
                            wafer.WaferId ?? "",
                            outputDisplay,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (displayCandidates.Count != 1 || !targetSet.Contains(displayCandidates[0]))
                {
                    reason = "Legacy Output parent를 하나로 확인할 수 없습니다. die=" +
                             (die.DieId ?? "") + ", wafer=" + outputDisplay +
                             ", candidates=" + displayCandidates.Count;
                    relatedDies.Clear();
                    return false;
                }

                relatedDies.Add(die);
            }

            relatedDies = relatedDies.Distinct().ToList();
            return true;
        }

        private static bool TryCollectInputParentDiesNoLock(
            ICollection<WaferMaterial> targetWafers,
            out List<DieMaterial> relatedDies,
            out string reason)
        {
            relatedDies = new List<DieMaterial>();
            reason = "";
            if (targetWafers == null || State == null || State.Wafers == null || State.Dies == null)
                return true;

            List<WaferMaterial> targets = targetWafers.Where(wafer => wafer != null).Distinct().ToList();
            if (targets.Count == 0)
                return true;

            var targetSet = new HashSet<WaferMaterial>(targets);
            var targetInstanceIds = new HashSet<string>(
                targets
                    .Select(EnsureWaferInstanceIdNoLock)
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            var targetDisplayIds = new HashSet<string>(
                targets
                    .Select(wafer => (wafer.WaferId ?? "").Trim())
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);

            foreach (string targetInstanceId in targetInstanceIds)
            {
                int candidateCount = State.Wafers.Count(wafer =>
                    wafer != null &&
                    string.Equals(
                        (wafer.WaferInstanceId ?? "").Trim(),
                        targetInstanceId,
                        StringComparison.OrdinalIgnoreCase));
                if (candidateCount != 1)
                {
                    reason = "Input parent instance를 하나로 확인할 수 없습니다. instance=" +
                             targetInstanceId + ", candidates=" + candidateCount;
                    return false;
                }
            }

            foreach (DieMaterial die in State.Dies)
            {
                if (die == null)
                    continue;

                string inputInstance = (die.InputWaferInstanceId ?? "").Trim();
                string inputDisplay = (die.WaferID_Input ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(inputInstance))
                {
                    if (!targetInstanceIds.Contains(inputInstance))
                        continue;

                    WaferMaterial parent = targets.FirstOrDefault(wafer =>
                        string.Equals(
                            (wafer.WaferInstanceId ?? "").Trim(),
                            inputInstance,
                            StringComparison.OrdinalIgnoreCase));
                    if (parent == null ||
                        (!string.IsNullOrWhiteSpace(inputDisplay) &&
                         !string.Equals(
                             inputDisplay,
                             parent.WaferId ?? "",
                             StringComparison.OrdinalIgnoreCase)))
                    {
                        reason = "Die의 Input instance/display parent가 일치하지 않습니다. die=" +
                                 (die.DieId ?? "") + ", instance=" + inputInstance +
                                 ", display=" + inputDisplay;
                        relatedDies.Clear();
                        return false;
                    }

                    relatedDies.Add(die);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(inputDisplay) || !targetDisplayIds.Contains(inputDisplay))
                    continue;

                List<WaferMaterial> displayCandidates = State.Wafers.Where(wafer =>
                        wafer != null &&
                        string.Equals(
                            wafer.WaferId ?? "",
                            inputDisplay,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (displayCandidates.Count != 1 || !targetSet.Contains(displayCandidates[0]))
                {
                    reason = "Legacy Input parent를 하나로 확인할 수 없습니다. die=" +
                             (die.DieId ?? "") + ", wafer=" + inputDisplay +
                             ", candidates=" + displayCandidates.Count;
                    relatedDies.Clear();
                    return false;
                }

                relatedDies.Add(die);
            }

            relatedDies = relatedDies.Distinct().ToList();
            return true;
        }

        private static bool TryCollectInputLocationClearDiesNoLock(
            ICollection<WaferMaterial> targetWafers,
            MaterialLocationKind expectedLocation,
            out List<DieMaterial> removeDies,
            out string reason,
            CassetteMaterialRole? cassetteRole = null,
            int? slotNumber = null)
        {
            List<DieMaterial> relatedDies;
            if (!TryCollectInputParentDiesNoLock(targetWafers, out relatedDies, out reason))
            {
                removeDies = new List<DieMaterial>();
                return false;
            }

            var relatedSet = new HashSet<DieMaterial>(relatedDies.Where(die => die != null));
            foreach (DieMaterial die in State.Dies)
            {
                if (die == null)
                    continue;

                MaterialLocationKind locationKind = die.CurrentLocation != null
                    ? die.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                if (locationKind == MaterialLocationKind.InputCassette &&
                    expectedLocation == MaterialLocationKind.InputCassette)
                {
                    List<CassetteMaterial> dieCassettes = State.Cassettes.Where(c =>
                        c != null && c.Role == die.CurrentLocation.CassetteRole).ToList();
                    if ((die.CurrentLocation.CassetteRole != CassetteMaterialRole.Input1 &&
                         die.CurrentLocation.CassetteRole != CassetteMaterialRole.Input2) ||
                        dieCassettes.Count != 1 || dieCassettes[0].Slots == null ||
                        die.CurrentLocation.SlotNumber < 0 || die.CurrentLocation.SlotNumber >= dieCassettes[0].Slots.Count)
                    {
                        reason = "입력 카세트 Die의 단/슬롯 위치가 올바르지 않습니다. die=" +
                                 (die.DieId ?? "") + ", location=" + die.CurrentLocation;
                        removeDies = new List<DieMaterial>();
                        return false;
                    }
                }

                // 정상 반납된 다른 단/슬롯의 Die는 이번 삭제 대상이 아니다.
                // 대상 범위 안의 parent 누락과 대상 Wafer에 연결된 활성 Die 검사는 유지한다.
                if (IsInputClearLocationInScope(die.CurrentLocation, expectedLocation, cassetteRole, slotNumber) &&
                    !relatedSet.Contains(die))
                {
                    reason = "입력 위치의 Die parent를 대상 Wafer로 확인할 수 없습니다. die=" +
                             (die.DieId ?? "") + ", location=" + die.CurrentLocation;
                    removeDies = new List<DieMaterial>();
                    return false;
                }
            }

            removeDies = new List<DieMaterial>();
            foreach (DieMaterial die in relatedDies)
            {
                MaterialLocationKind locationKind = die.CurrentLocation != null
                    ? die.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                if (expectedLocation == MaterialLocationKind.InputCassette &&
                    locationKind == MaterialLocationKind.InputCassette)
                {
                    string parentInstance = (die.InputWaferInstanceId ?? "").Trim();
                    WaferMaterial parent = targetWafers.FirstOrDefault(wafer => wafer != null &&
                        (!string.IsNullOrWhiteSpace(parentInstance)
                            ? string.Equals((wafer.WaferInstanceId ?? "").Trim(), parentInstance, StringComparison.OrdinalIgnoreCase)
                            : string.Equals((wafer.WaferId ?? "").Trim(), (die.WaferID_Input ?? "").Trim(), StringComparison.OrdinalIgnoreCase)));
                    if (parent == null || !IsWaferAtCassetteSlot(
                        parent, die.CurrentLocation.CassetteRole, die.CurrentLocation.SlotNumber))
                    {
                        reason = "입력 카세트 Die와 parent Wafer의 단/슬롯 위치가 일치하지 않습니다. die=" +
                                 (die.DieId ?? "") + ", location=" + die.CurrentLocation;
                        removeDies.Clear();
                        return false;
                    }
                }
                bool transitionalInputStage =
                    expectedLocation == MaterialLocationKind.InputFeeder &&
                    locationKind == MaterialLocationKind.InputStage;
                bool removeAtLocation = IsInputClearLocationInScope(
                                            die.CurrentLocation, expectedLocation, cassetteRole, slotNumber) ||
                                        locationKind == MaterialLocationKind.Unknown ||
                                        transitionalInputStage;

                bool hasOutputParent =
                    !string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) ||
                    !string.IsNullOrWhiteSpace(die.WaferID_Output);
                bool hasActiveReservation =
                    die.ReservedPickerLocation == MaterialLocationKind.PickerFront ||
                    die.ReservedPickerLocation == MaterialLocationKind.PickerRear ||
                    die.ReservedPickerNo > 0;
                if (hasActiveReservation)
                {
                    reason = "입력 Clear 대상 Die가 Output parent/Picker 예약을 유지하고 있습니다. die=" +
                             (die.DieId ?? "") + ", location=" + locationKind +
                             ", outputWafer=" + (die.WaferID_Output ?? "") +
                             ", reserved=" + die.ReservedPickerLocation + "/" + die.ReservedPickerNo;
                    removeDies.Clear();
                    return false;
                }

                if (hasOutputParent)
                    continue;
                if (!removeAtLocation)
                {
                    reason = "입력 Clear 대상 Wafer의 input-only Die가 다른 활성 위치에 있습니다. die=" +
                             (die.DieId ?? "") + ", expected=" + expectedLocation +
                             ", location=" + locationKind;
                    removeDies.Clear();
                    return false;
                }

                removeDies.Add(die);
            }

            return true;
        }

        private static bool IsInputClearLocationInScope(
            MaterialLocation location,
            MaterialLocationKind expectedLocation,
            CassetteMaterialRole? cassetteRole,
            int? slotNumber)
        {
            if (location == null || location.Kind != expectedLocation)
                return false;
            if (expectedLocation != MaterialLocationKind.InputCassette)
                return true;
            return (!cassetteRole.HasValue || location.CassetteRole == cassetteRole.Value) &&
                   (!slotNumber.HasValue || location.SlotNumber == slotNumber.Value);
        }

        private static bool TryCollectInputHistoryForClearNoLock(
            ICollection<WaferMaterial> targetWafers,
            out List<DieMaterial> historyDies,
            out bool preserveHistory,
            out string reason)
        {
            if (!TryCollectInputParentDiesNoLock(targetWafers, out historyDies, out reason))
            {
                preserveHistory = false;
                return false;
            }

            preserveHistory = historyDies.Any(die =>
                die != null &&
                (!string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) ||
                 !string.IsNullOrWhiteSpace(die.WaferID_Output)));
            return true;
        }

        private static void DemotePreservedInputHistoryLocationsNoLock(
            ICollection<DieMaterial> historyDies,
            MaterialLocationKind clearedLocation)
        {
            if (historyDies == null)
                return;

            DateTime updatedAt = DateTime.Now;
            foreach (DieMaterial die in historyDies.Where(item => item != null).Distinct())
            {
                MaterialLocationKind locationKind = die.CurrentLocation != null
                    ? die.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                bool clearedPhysicalLocation = locationKind == clearedLocation ||
                    (clearedLocation == MaterialLocationKind.InputFeeder &&
                     locationKind == MaterialLocationKind.InputStage);
                if (!clearedPhysicalLocation)
                    continue;

                die.CurrentLocation = MaterialLocation.Unknown();
                die.UpdatedAt = updatedAt;
            }
        }

        private static bool TryCollectOutputLocationClearDiesNoLock(
            ICollection<WaferMaterial> targetWafers,
            MaterialLocationKind expectedLocation,
            out List<DieMaterial> relatedDies,
            out string reason)
        {
            if (!TryCollectOutputParentDiesNoLock(targetWafers, out relatedDies, out reason))
                return false;

            var relatedSet = new HashSet<DieMaterial>(relatedDies.Where(die => die != null));
            foreach (DieMaterial die in State.Dies)
            {
                if (die == null)
                    continue;

                MaterialLocationKind locationKind = die.CurrentLocation != null
                    ? die.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                if (locationKind == expectedLocation && !relatedSet.Contains(die))
                {
                    reason = "출력 위치의 Die parent를 대상 Wafer로 확인할 수 없습니다. die=" +
                             (die.DieId ?? "") + ", location=" + expectedLocation;
                    relatedDies.Clear();
                    return false;
                }
            }

            foreach (DieMaterial die in relatedDies)
            {
                MaterialLocationKind locationKind = die.CurrentLocation != null
                    ? die.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                bool hasActiveReservation =
                    die.ReservedPickerLocation == MaterialLocationKind.PickerFront ||
                    die.ReservedPickerLocation == MaterialLocationKind.PickerRear ||
                    die.ReservedPickerNo > 0;
                if ((locationKind != MaterialLocationKind.Unknown && locationKind != expectedLocation) ||
                    hasActiveReservation)
                {
                    reason = "출력 Clear 대상 Die가 다른 활성 위치/예약에 있습니다. die=" +
                             (die.DieId ?? "") + ", expected=" + expectedLocation +
                             ", location=" + locationKind +
                             ", reserved=" + die.ReservedPickerLocation + "/" + die.ReservedPickerNo;
                    relatedDies.Clear();
                    return false;
                }
            }

            List<DieMaterial> preserveDies;
            List<DieMaterial> removeDies;
            return TryClassifyOutputDiesForDetachNoLock(
                relatedDies,
                out preserveDies,
                out removeDies,
                out reason);
        }

        public static bool ClearInputCassetteSlotData(CassetteMaterialRole cassetteRole, int slotNumber)
        {
            string reason;
            return ClearInputCassetteSlotData(cassetteRole, slotNumber, out reason);
        }

        /// <summary>
        /// Input Cassette 단일 Slot Data 초기화. 차단 시 사유를 그대로 돌려준다(전체 초기화와 동일 정책).
        /// </summary>
        public static bool ClearInputCassetteSlotData(CassetteMaterialRole cassetteRole, int slotNumber, out string reason)
        {
            reason = "";
            MaterialCompactionResult compactionResult;
            lock (_stateSync)
            {
                var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
                if (cassette == null)
                {
                    reason = "대상 Cassette가 자재 상태에 없습니다. cassette=" + cassetteRole;
                    LogMaterialClearBlocked("Input cassette slot clear", reason);
                    return false;
                }

                string preflightReason;
                if (!TryValidateCassetteRoleForClearNoLock(cassetteRole, out preflightReason))
                {
                    reason = preflightReason;
                    LogMaterialClearBlocked("Input cassette slot clear", preflightReason);
                    return false;
                }

                cassette.EnsureSlots();
                if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                {
                    reason = "Slot 번호가 범위를 벗어났습니다. cassette=" + cassetteRole +
                             ", slot=" + (slotNumber + 1) + ", slotCount=" + cassette.Slots.Count;
                    LogMaterialClearBlocked("Input cassette slot clear", reason);
                    return false;
                }

                var slot = cassette.Slots[slotNumber];
                WaferMaterial slotWafer = null;
                if (slot != null && slot.HasWafer)
                {
                    string slotReason;
                    slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                    if (slotWafer == null)
                    {
                        reason = slotReason + ", cassette=" + cassetteRole + ", slot=" + (slotNumber + 1);
                        LogMaterialClearBlocked("Input cassette slot clear", reason);
                        return false;
                    }
                }
                string slotInstanceId = slotWafer != null
                    ? EnsureWaferInstanceIdNoLock(slotWafer)
                    : "";
                var targetWafers = State.Wafers.Where(w =>
                    w != null &&
                    ((!string.IsNullOrWhiteSpace(slotInstanceId) &&
                      string.Equals(
                          EnsureWaferInstanceIdNoLock(w),
                          slotInstanceId,
                          StringComparison.OrdinalIgnoreCase)) ||
                     IsWaferAtCassetteSlot(w, cassetteRole, slotNumber)))
                    .ToList();

                List<DieMaterial> inputOnlyDies;
                string inputDieReason;
                if (!TryCollectInputLocationClearDiesNoLock(
                    targetWafers,
                    MaterialLocationKind.InputCassette,
                    out inputOnlyDies,
                    out inputDieReason,
                    cassetteRole,
                    slotNumber))
                {
                    reason = inputDieReason;
                    LogMaterialClearBlocked("Input cassette slot clear", inputDieReason);
                    return false;
                }
                List<DieMaterial> inputHistoryDies;
                bool preserveInputHistory;
                if (!TryCollectInputHistoryForClearNoLock(
                    targetWafers,
                    out inputHistoryDies,
                    out preserveInputHistory,
                    out inputDieReason))
                {
                    reason = inputDieReason;
                    LogMaterialClearBlocked("Input cassette slot clear", inputDieReason);
                    return false;
                }

                if (preserveInputHistory)
                    DemotePreservedInputHistoryLocationsNoLock(
                        inputHistoryDies,
                        MaterialLocationKind.InputCassette);
                else
                    RemoveDieMaterialsNoLock(inputHistoryDies);

                foreach (var wafer in targetWafers)
                {
                    wafer.State = WaferMaterialState.Empty;
                    wafer.CurrentLocation = MaterialLocation.Unknown();
                    // To do: 슬롯 자재를 비울 때 CassetteLotId도 함께 지워야 한다.
                    // 이 값을 남기면 다음 mapping의 ResolveOrCreateCassetteLotId가
                    // State.LotId와 다른 잔존 LotId를 후보로 잡아 "LOT ID 후보가 서로 달라..." 예외로 등록 실패한다.
                    wafer.CassetteLotId = "";
                    wafer.UpdatedAt = DateTime.Now;
                }

                slot.WaferId = "";
                slot.WaferInstanceId = "";
                slot.HasWafer = false;
                ResetCassetteMappingIfEmptyNoLock(cassette, "ClearInputCassetteSlotData");
                compactionResult = CompactMaterialStateNoLock();
            }
            LogMaterialCompaction("ClearInputCassetteSlotData", compactionResult);
            NotifyAndSave("ClearInputCassetteSlotData");
            return true;
        }

        /// <summary>
        /// [정합성 2026-08-10] 슬롯을 하나씩 지워 카세트가 완전히 비게 된 경우에도
        /// 전체 Clear(ClearInput/OutputCassetteAllSlotData)와 동일하게 mapping 상태를 해제한다.
        ///
        /// 배경: 단일 슬롯 Clear 경로는 IsMapped/IsPresent를 건드리지 않아서, 13슬롯을 하나씩 비우면
        ///   "mapped=True, present=True 인데 점유 0"인 모순 상태가 남았다. 이 상태는 매핑이 끝난 것처럼
        ///   보이지만 작업할 Ready Wafer가 없어 Auto 시작이 SEQ-IN-NO-READY-WAFER로 멈춘다
        ///   (2026-08-10 실측: Input1[mapped=True, slots=13, materialOccupied=0]).
        ///   IsPresent도 물리 센서가 아니라 mapping으로 만든 논리 상태이므로 함께 초기화한다.
        ///
        /// 슬롯이 하나라도 남아 있으면 mapping은 여전히 유효하므로 아무것도 바꾸지 않는다.
        /// </summary>
        private static void ResetCassetteMappingIfEmptyNoLock(CassetteMaterial cassette, string reason)
        {
            try
            {
                if (cassette == null || cassette.Slots == null)
                    return;

                if (!cassette.IsMapped && !cassette.IsPresent)
                    return;

                foreach (CassetteSlotMaterial slot in cassette.Slots)
                {
                    if (slot == null)
                        continue;

                    // 점유 슬롯이 하나라도 남아 있으면 mapping 결과는 그대로 유효하다.
                    if (slot.HasWafer ||
                        !string.IsNullOrWhiteSpace(slot.WaferId) ||
                        !string.IsNullOrWhiteSpace(slot.WaferInstanceId))
                    {
                        return;
                    }
                }

                cassette.IsMapped = false;
                cassette.IsPresent = false;
                Log.Write(LogLevel.AboveNormal, "Main", "MaterialStateService",
                    "슬롯을 모두 비워 카세트 mapping 상태를 해제했습니다. 다음 Auto 시작 전에 다시 매핑해야 합니다. cassette=" +
                    cassette.Role + ", reason=" + (reason ?? "") + " - Ok");
            }
            catch (Exception ex)
            {
                Log.Write(LogLevel.AboveNormal, "Main", "MaterialStateService",
                    "카세트 mapping 상태 해제 확인 실패: " + ex.Message + " - Failed");
            }
        }

        public static bool ClearInputCassetteAllSlotData()
        {
            string reason;
            return ClearInputCassetteAllSlotData(out reason);
        }

        /// <summary>
        /// Input Cassette 전체 Data 초기화. 차단 시 사유를 그대로 돌려준다.
        /// [사용자 확정 2026-08-17] 기존에는 bool만 반환해, 호출 화면이 "초기화 차단"과
        ///   "저장 실패"를 구분하지 못하고 동일 문구("저장 파일까지 초기화하지 못했습니다")를
        ///   띄웠다. 실제로는 사전검사에서 막혀 저장은 시도조차 하지 않은 경우가 대부분이다.
        /// </summary>
        public static bool ClearInputCassetteAllSlotData(out string reason)
        {
            return ClearInputCassetteAllSlotDataCore(false, out reason);
        }

        public static bool CanCompleteInputCassetteExchange(out string reason)
        {
            lock (_stateSync)
            {
                return TryValidateInputCassetteExchangeNoLock(out reason) &&
                       TryValidateCassetteRoleForClearNoLock(CassetteMaterialRole.Input1, out reason) &&
                       TryValidateCassetteRoleForClearNoLock(CassetteMaterialRole.Input2, out reason);
            }
        }

        /// <summary>
        /// 현재 Picker 보유 위치 또는 Picker 예약으로 남은 Die 데이터가 없는지 확인합니다.
        /// Output Stage/Feeder/Cassette의 정상 Die 이력은 이 검사 대상이 아닙니다.
        /// </summary>
        internal static bool TryValidatePickerProductDataEmpty(out string reason)
        {
            lock (_stateSync)
                return TryValidatePickerProductDataEmptyNoLock(out reason);
        }

        private static bool TryValidatePickerProductDataEmptyNoLock(out string reason)
        {
            reason = string.Empty;
            if (State == null || State.Dies == null)
            {
                reason = "Picker 제품 유무를 확인할 Material 상태가 없습니다.";
                return false;
            }

            foreach (DieMaterial die in State.Dies)
            {
                if (die == null || die.CurrentLocation == null)
                {
                    reason = "Die 위치 데이터가 없어 Picker 제품 유무를 확인할 수 없습니다.";
                    return false;
                }

                MaterialLocationKind location = die.CurrentLocation.Kind;
                bool reserved = die.ReservedPickerLocation == MaterialLocationKind.PickerFront ||
                                die.ReservedPickerLocation == MaterialLocationKind.PickerRear ||
                                die.ReservedPickerNo > 0;
                if (reserved || location == MaterialLocationKind.PickerFront ||
                    location == MaterialLocationKind.PickerRear)
                {
                    reason = "Picker 보유/예약 Die 데이터가 남아 있습니다. die=" +
                             (die.DieId ?? string.Empty) + ", location=" + location + ", picker=" +
                             (die.CurrentLocation.PickerNo > 0 ? die.CurrentLocation.PickerNo.ToString() : "-") +
                             ", reserved=" + die.ReservedPickerLocation + "/" + die.ReservedPickerNo +
                             ". 실물이 없다면 위치/예약 데이터 불일치를 먼저 복구하십시오.";
                    return false;
                }
            }
            return true;
        }

        public static bool ClearInputCassetteForExchange(out string reason)
        {
            return ClearInputCassetteAllSlotDataCore(true, out reason);
        }

        private static bool TryValidateInputCassetteExchangeNoLock(out string reason)
        {
            reason = "";
            if (State == null || State.Cassettes == null || State.Wafers == null || State.Dies == null)
            {
                reason = "입력 카세트 교체에 필요한 Material 상태가 없습니다.";
                return false;
            }
            if (!State.Cassettes.Any(c => c != null &&
                (c.Role == CassetteMaterialRole.Input1 || c.Role == CassetteMaterialRole.Input2)))
            {
                reason = "초기화 대상 Input Cassette(Input1/Input2)가 자재 상태에 없습니다.";
                return false;
            }

            foreach (WaferMaterial wafer in State.Wafers)
            {
                if (wafer == null || wafer.CurrentLocation == null)
                {
                    reason = "Wafer 위치 데이터가 없어 입력 카세트 교체 가능 여부를 확인할 수 없습니다.";
                    return false;
                }
                MaterialLocationKind location = wafer.CurrentLocation.Kind;
                WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
                if (location == MaterialLocationKind.InputStage || location == MaterialLocationKind.InputFeeder)
                {
                    reason = "저장된 Material에 입력 이송 위치의 Wafer가 남아 있습니다. wafer=" +
                             (wafer.WaferId ?? "") + ", location=" + location + ", state=" + state +
                             ". 실물이 없다면 위치 데이터 불일치를 복구한 후 CST CLEAR를 실행하십시오.";
                    return false;
                }
                if (location == MaterialLocationKind.InputCassette &&
                    state != WaferMaterialState.Empty && state != WaferMaterialState.Finish)
                {
                    reason = "입력 카세트에 Finish가 아닌 Wafer가 있습니다. cassette=" +
                             wafer.CurrentLocation.CassetteRole + ", slot=" +
                             (wafer.CurrentLocation.SlotNumber + 1) + ", wafer=" + (wafer.WaferId ?? "") +
                             ", state=" + state + ". 정상 반납과 작업 완료를 확인하십시오.";
                    return false;
                }
                if (location == MaterialLocationKind.Unknown && state != WaferMaterialState.Empty)
                {
                    reason = "현재 위치가 확인되지 않은 Wafer가 있습니다. wafer=" +
                             (wafer.WaferId ?? "") + ", state=" + state + ". 위치 데이터를 먼저 복구하십시오.";
                    return false;
                }
            }

            if (!TryValidatePickerProductDataEmptyNoLock(out reason))
                return false;

            foreach (DieMaterial die in State.Dies)
            {
                if (die == null || die.CurrentLocation == null)
                {
                    reason = "Die 위치 데이터가 없어 입력 카세트 교체 가능 여부를 확인할 수 없습니다.";
                    return false;
                }
                MaterialLocationKind location = die.CurrentLocation.Kind;
                // Output 이력 연결이 있어도 입력 이송 중인 Die는 완료 이력이 아니다.
                if (location == MaterialLocationKind.InputStage || location == MaterialLocationKind.InputFeeder)
                {
                    reason = "입력 이송 Die 데이터가 남아 있습니다. die=" +
                             (die.DieId ?? "") + ", location=" + location +
                             ". 실물이 없다면 위치/예약 데이터 불일치를 복구하십시오.";
                    return false;
                }
            }
            return true;
        }

        private static bool ClearInputCassetteAllSlotDataCore(bool requireFinishedExchange, out string reason)
        {
            reason = "";
            bool processed = false;
            MaterialCompactionResult compactionResult;
            lock (_stateSync)
            {
                // 확인창 이후 바뀐 Material도 삭제와 같은 잠금 안에서 다시 검사한다.
                if (requireFinishedExchange && !TryValidateInputCassetteExchangeNoLock(out reason))
                {
                    LogMaterialClearBlocked("Input cassette exchange clear", reason);
                    return false;
                }
                string preflightReason;
                if (!TryValidateCassetteRoleForClearNoLock(CassetteMaterialRole.Input1, out preflightReason) ||
                    !TryValidateCassetteRoleForClearNoLock(CassetteMaterialRole.Input2, out preflightReason))
                {
                    reason = preflightReason;
                    LogMaterialClearBlocked("Input cassette all clear", preflightReason);
                    return false;
                }

                ClearInputCassetteAllSlotData(CassetteMaterialRole.Input1, ref processed);
                ClearInputCassetteAllSlotData(CassetteMaterialRole.Input2, ref processed);

                if (!processed)
                {
                    reason = "초기화 대상 Input Cassette(Input1/Input2)가 자재 상태에 없습니다.";
                    LogMaterialClearBlocked("Input cassette all clear", reason);
                    return false;
                }

                compactionResult = CompactMaterialStateNoLock();
            }
            LogMaterialCompaction("ClearInputCassetteAllSlotData", compactionResult);
            NotifyAndSave("ClearInputCassetteAllSlotData");
            return true;
        }

        private static void ClearInputCassetteAllSlotData(CassetteMaterialRole cassetteRole, ref bool processed)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null)
                return;

            cassette.EnsureSlots();
            processed = true;

            var slotWaferInstanceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CassetteSlotMaterial slot in cassette.Slots)
            {
                if (slot == null || !slot.HasWafer)
                    continue;
                string slotReason;
                WaferMaterial slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                if (slotWafer != null)
                    slotWaferInstanceIds.Add(EnsureWaferInstanceIdNoLock(slotWafer));
            }

            var targetWafers = State.Wafers.Where(w =>
                w != null &&
                ((slotWaferInstanceIds.Count > 0 &&
                  slotWaferInstanceIds.Contains(EnsureWaferInstanceIdNoLock(w))) ||
                 (w.CurrentLocation != null &&
                   w.CurrentLocation.Kind == MaterialLocationKind.InputCassette &&
                   w.CurrentLocation.CassetteRole == cassetteRole)))
                .ToList();

            List<DieMaterial> inputHistoryDies;
            bool preserveInputHistory;
            string inputReason;
            if (!TryCollectInputHistoryForClearNoLock(
                targetWafers,
                out inputHistoryDies,
                out preserveInputHistory,
                out inputReason))
            {
                throw new InvalidOperationException(inputReason);
            }
            if (preserveInputHistory)
                DemotePreservedInputHistoryLocationsNoLock(
                    inputHistoryDies,
                    MaterialLocationKind.InputCassette);
            else
                RemoveDieMaterialsNoLock(inputHistoryDies);

            foreach (var wafer in targetWafers)
            {
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                // To do: 전체 삭제 시 웨이퍼 CassetteLotId도 비워야 잔존 LotId가 다음 mapping을 막지 않는다.
                wafer.CassetteLotId = "";
                wafer.UpdatedAt = DateTime.Now;
            }

            foreach (var slot in cassette.Slots)
            {
                if (slot == null)
                    continue;

                slot.WaferId = "";
                slot.WaferInstanceId = "";
                slot.HasWafer = false;
            }

            // Material slot data를 모두 지운 뒤에는 마지막 mapping 결과를 더 이상
            // 유효한 것으로 사용할 수 없다. 다음 Auto 시작에서 실제 mapping을 다시
            // 수행하여 센서 결과와 Ready Material을 함께 재생성하도록 한다.
            cassette.IsMapped = false;
            // IsPresent는 현재 물리 센서가 아니라 Mapping으로 만든 논리 상태다.
            // 모든 Slot Data를 삭제한 뒤 true를 남기면 Present/Unmapped 잔재가
            // Recipe 변경을 영구 차단하므로 다음 Mapping 전까지 false로 초기화한다.
            cassette.IsPresent = false;

            // To do: 카세트 레코드의 CassetteLotId도 초기화해야 한다.
            // 이 값(예: Y482CB12)이 State.LotId(예: Y482CB1)와 달라지면
            // 슬롯을 모두 비운 뒤에도 ResolveOrCreateCassetteLotId가 후보 2개로 인식해
            // "카세트 LOT ID 후보가 서로 달라 LOT ID를 결정할 수 없습니다." 예외로 재mapping이 막힌다.
            cassette.CassetteLotId = "";
        }

        public static bool ClearOutputCassetteSlotData(CassetteMaterialRole cassetteRole, int slotNumber)
        {
            if (cassetteRole != CassetteMaterialRole.Good1 &&
                cassetteRole != CassetteMaterialRole.Good2 &&
                cassetteRole != CassetteMaterialRole.Ng1)
                return false;

            MaterialCompactionResult compactionResult;
            lock (_stateSync)
            {
                var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
                if (cassette == null)
                    return false;

                string preflightReason;
                if (!TryValidateCassetteRoleForClearNoLock(cassetteRole, out preflightReason))
                {
                    LogMaterialClearBlocked("Output cassette slot clear", preflightReason);
                    return false;
                }

                cassette.EnsureSlots();
                if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                    return false;

                var slot = cassette.Slots[slotNumber];
                WaferMaterial slotWafer = null;
                if (slot != null && slot.HasWafer)
                {
                    string slotReason;
                    slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                    if (slotWafer == null)
                    {
                        LogMaterialClearBlocked("Output cassette slot clear",
                            slotReason + ", cassette=" + cassetteRole + ", slot=" + (slotNumber + 1));
                        return false;
                    }
                }
                string slotInstanceId = slotWafer != null
                    ? EnsureWaferInstanceIdNoLock(slotWafer)
                    : "";
                var targetWafers = State.Wafers.Where(w =>
                    w != null &&
                    ((!string.IsNullOrWhiteSpace(slotInstanceId) &&
                      string.Equals(
                          EnsureWaferInstanceIdNoLock(w),
                          slotInstanceId,
                          StringComparison.OrdinalIgnoreCase)) ||
                     IsWaferAtCassetteSlot(w, cassetteRole, slotNumber)))
                    .ToList();

                List<DieMaterial> relatedDies;
                string dieReason;
                if (!TryCollectOutputCassetteClearDiesNoLock(
                    targetWafers,
                    out relatedDies,
                    out dieReason))
                {
                    LogMaterialClearBlocked("Output cassette slot clear", dieReason);
                    return false;
                }

                foreach (var wafer in targetWafers)
                {
                    ClearOutputStageWaferProcessingFieldsNoLock(wafer);
                    wafer.State = WaferMaterialState.Empty;
                    wafer.CurrentLocation = MaterialLocation.Unknown();
                    wafer.CassetteLotId = "";
                    wafer.UpdatedAt = DateTime.Now;
                }

                DetachOrRemoveClearedOutputDiesNoLock(relatedDies);
                slot.WaferId = "";
                slot.WaferInstanceId = "";
                slot.HasWafer = false;
                ResetCassetteMappingIfEmptyNoLock(cassette, "ClearOutputCassetteSlotData");
                compactionResult = CompactMaterialStateNoLock();
            }
            LogMaterialCompaction("ClearOutputCassetteSlotData", compactionResult);
            NotifyAndSave("ClearOutputCassetteSlotData");
            return true;
        }

        public static bool ClearOutputCassetteAllSlotData()
        {
            bool processed = false;
            MaterialCompactionResult compactionResult;
            lock (_stateSync)
            {
                string preflightReason;
                if (!TryValidateCassetteRoleForClearNoLock(CassetteMaterialRole.Good1, out preflightReason) ||
                    !TryValidateCassetteRoleForClearNoLock(CassetteMaterialRole.Good2, out preflightReason) ||
                    !TryValidateCassetteRoleForClearNoLock(CassetteMaterialRole.Ng1, out preflightReason))
                {
                    LogMaterialClearBlocked("Output cassette all clear", preflightReason);
                    return false;
                }

                ClearOutputCassetteAllSlotData(CassetteMaterialRole.Good1, ref processed);
                ClearOutputCassetteAllSlotData(CassetteMaterialRole.Good2, ref processed);
                ClearOutputCassetteAllSlotData(CassetteMaterialRole.Ng1, ref processed);

                if (!processed)
                    return false;

                compactionResult = CompactMaterialStateNoLock();
            }
            LogMaterialCompaction("ClearOutputCassetteAllSlotData", compactionResult);
            NotifyAndSave("ClearOutputCassetteAllSlotData");
            return true;
        }

        /// <summary>
        /// 출력 카세트를 GOOD / NG 한쪽만 골라서 Material 데이터를 초기화한다.
        /// 카세트 교체는 GOOD만 또는 NG만 진행하는 경우가 많아 반대편 데이터를 보존해야 한다.
        /// GOOD은 장비에서 한 묶음으로 취급하므로 Good1/Good2를 함께 지운다(사용자 확정 2026-07-26).
        /// </summary>
        public static bool ClearOutputCassetteSideData(QMC.CDT320.BinSide side)
        {
            try
            {
                bool processed = false;
                MaterialCompactionResult compactionResult;
                lock (_stateSync)
                {
                    string preflightReason;
                    if (side == QMC.CDT320.BinSide.Ng)
                    {
                        if (!TryValidateCassetteRoleForClearNoLock(CassetteMaterialRole.Ng1, out preflightReason))
                        {
                            LogMaterialClearBlocked("Output cassette side clear", preflightReason);
                            return false;
                        }
                        ClearOutputCassetteAllSlotData(CassetteMaterialRole.Ng1, ref processed);
                    }
                    else
                    {
                        if (!TryValidateCassetteRoleForClearNoLock(CassetteMaterialRole.Good1, out preflightReason) ||
                            !TryValidateCassetteRoleForClearNoLock(CassetteMaterialRole.Good2, out preflightReason))
                        {
                            LogMaterialClearBlocked("Output cassette side clear", preflightReason);
                            return false;
                        }
                        ClearOutputCassetteAllSlotData(CassetteMaterialRole.Good1, ref processed);
                        ClearOutputCassetteAllSlotData(CassetteMaterialRole.Good2, ref processed);
                    }

                    if (!processed)
                        return false;

                    compactionResult = CompactMaterialStateNoLock();
                }

                LogMaterialCompaction("ClearOutputCassetteSideData:" + side, compactionResult);
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "출력 카세트 " + side + " 측 Material 데이터를 초기화했습니다. " +
                    "반대편 데이터는 유지됩니다. - Ok");
                NotifyAndSave("ClearOutputCassetteSideData:" + side);
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "출력 카세트 " + side + " 측 Material 초기화 실패: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static void ClearOutputCassetteAllSlotData(CassetteMaterialRole cassetteRole, ref bool processed)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null)
                return;

            cassette.EnsureSlots();
            processed = true;

            var slotWaferInstanceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CassetteSlotMaterial slot in cassette.Slots)
            {
                if (slot == null || !slot.HasWafer)
                    continue;
                string slotReason;
                WaferMaterial slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                if (slotWafer != null)
                    slotWaferInstanceIds.Add(EnsureWaferInstanceIdNoLock(slotWafer));
            }

            var targetWafers = State.Wafers.Where(w =>
                w != null &&
                ((slotWaferInstanceIds.Count > 0 &&
                  slotWaferInstanceIds.Contains(EnsureWaferInstanceIdNoLock(w))) ||
                 (w.CurrentLocation != null &&
                    w.CurrentLocation.Kind == MaterialLocationKind.OutputCassette &&
                   w.CurrentLocation.CassetteRole == cassetteRole)))
                .ToList();

            List<DieMaterial> relatedDies;
            string dieReason;
            if (!TryCollectOutputCassetteClearDiesNoLock(
                targetWafers,
                out relatedDies,
                out dieReason))
            {
                throw new InvalidOperationException(dieReason);
            }

            foreach (var wafer in targetWafers)
            {
                ClearOutputStageWaferProcessingFieldsNoLock(wafer);
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.CassetteLotId = "";
                wafer.UpdatedAt = DateTime.Now;
            }

            DetachOrRemoveClearedOutputDiesNoLock(relatedDies);

            foreach (var slot in cassette.Slots)
            {
                if (slot == null)
                    continue;

                slot.WaferId = "";
                slot.WaferInstanceId = "";
                slot.HasWafer = false;
            }

            // Material slot data를 모두 지운 뒤에는 마지막 mapping 결과를 더 이상
            // 유효한 것으로 사용할 수 없다. 다음 전체 준비에서 실제 mapping을 다시
            // 수행하여 센서 결과와 Ready Material을 함께 재생성하도록 한다.
            cassette.IsMapped = false;
            // 출력 카세트도 Mapping 결과가 IsPresent를 다시 설정한다.
            // Side/전체 Clear 직후에는 빈 논리 상태로 내려 잔존 Material 판정을 막는다.
            cassette.IsPresent = false;
            cassette.CassetteLotId = "";
        }

        private static List<WaferMaterial> GetCassetteClearTargetWafersNoLock(
            CassetteMaterialRole cassetteRole)
        {
            var targets = new HashSet<WaferMaterial>();
            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == cassetteRole);
            if (cassette != null && cassette.Slots != null)
            {
                foreach (CassetteSlotMaterial slot in cassette.Slots)
                {
                    if (slot == null || !slot.HasWafer)
                        continue;

                    string resolveReason;
                    WaferMaterial slotWafer = ResolveCassetteSlotWaferForValidationNoLock(slot, out resolveReason);
                    if (slotWafer != null)
                        targets.Add(slotWafer);
                }
            }

            MaterialLocationKind expectedKind = IsOutputCassetteRole(cassetteRole)
                ? MaterialLocationKind.OutputCassette
                : MaterialLocationKind.InputCassette;
            foreach (WaferMaterial wafer in State.Wafers)
            {
                if (wafer != null &&
                    wafer.CurrentLocation != null &&
                    wafer.CurrentLocation.Kind == expectedKind &&
                    wafer.CurrentLocation.CassetteRole == cassetteRole)
                {
                    targets.Add(wafer);
                }
            }

            return targets.ToList();
        }

        private static bool TryCollectOutputCassetteClearDiesNoLock(
            ICollection<WaferMaterial> targetWafers,
            out List<DieMaterial> relatedDies,
            out string reason)
        {
            if (!TryCollectOutputParentDiesNoLock(targetWafers, out relatedDies, out reason))
                return false;

            List<WaferMaterial> targets = targetWafers != null
                ? targetWafers.Where(wafer => wafer != null).Distinct().ToList()
                : new List<WaferMaterial>();
            var relatedSet = new HashSet<DieMaterial>(relatedDies.Where(die => die != null));
            foreach (DieMaterial die in State.Dies)
            {
                if (die == null)
                    continue;

                MaterialLocation location = die.CurrentLocation;
                MaterialLocationKind locationKind = location != null
                    ? location.Kind
                    : MaterialLocationKind.Unknown;
                bool atExpectedCassette = locationKind == MaterialLocationKind.OutputCassette &&
                    targets.Any(wafer =>
                        wafer.CurrentLocation != null &&
                        wafer.CurrentLocation.Kind == MaterialLocationKind.OutputCassette &&
                        wafer.CurrentLocation.CassetteRole == location.CassetteRole &&
                        wafer.CurrentLocation.SlotNumber == location.SlotNumber);
                if (atExpectedCassette && !relatedSet.Contains(die))
                {
                    reason = "출력 Cassette 위치의 Die parent를 대상 Wafer로 확인할 수 없습니다. die=" +
                             (die.DieId ?? "") + ", location=" + location;
                    relatedDies.Clear();
                    return false;
                }
            }

            foreach (DieMaterial die in relatedDies)
            {
                MaterialLocation location = die.CurrentLocation;
                MaterialLocationKind locationKind = location != null
                    ? location.Kind
                    : MaterialLocationKind.Unknown;
                bool atExpectedCassette = locationKind == MaterialLocationKind.OutputCassette &&
                    targets.Any(wafer =>
                        wafer.CurrentLocation != null &&
                        wafer.CurrentLocation.Kind == MaterialLocationKind.OutputCassette &&
                        wafer.CurrentLocation.CassetteRole == location.CassetteRole &&
                        wafer.CurrentLocation.SlotNumber == location.SlotNumber);
                bool hasActiveReservation =
                    die.ReservedPickerLocation == MaterialLocationKind.PickerFront ||
                    die.ReservedPickerLocation == MaterialLocationKind.PickerRear ||
                    die.ReservedPickerNo > 0;
                if ((locationKind != MaterialLocationKind.Unknown && !atExpectedCassette) ||
                    hasActiveReservation)
                {
                    reason = "출력 Cassette Clear 대상 Die가 다른 활성 위치/예약에 있습니다. die=" +
                             (die.DieId ?? "") + ", location=" + locationKind +
                             ", reserved=" + die.ReservedPickerLocation + "/" + die.ReservedPickerNo;
                    relatedDies.Clear();
                    return false;
                }
            }

            List<DieMaterial> preserveDies;
            List<DieMaterial> removeDies;
            return TryClassifyOutputDiesForDetachNoLock(
                relatedDies,
                out preserveDies,
                out removeDies,
                out reason);
        }

        private static bool TryValidateCassetteRoleForClearNoLock(
            CassetteMaterialRole cassetteRole,
            out string reason)
        {
            reason = "";
            if (!IsOutputCassetteRole(cassetteRole) && !TryValidateInputCassetteWaferPointersNoLock(out reason))
                return false;
            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == cassetteRole);
            if (cassette == null)
                return true;

            if (cassette.Slots == null || cassette.SlotCount < 0 || cassette.Slots.Count != cassette.SlotCount)
            {
                reason = "Cassette Slot 목록과 SlotCount가 일치하지 않습니다. cassette=" + cassetteRole +
                         ", slotCount=" + cassette.SlotCount +
                         ", items=" + (cassette.Slots != null ? cassette.Slots.Count : -1);
                return false;
            }

            for (int slotIndex = 0; slotIndex < cassette.Slots.Count; slotIndex++)
            {
                CassetteSlotMaterial slot = cassette.Slots[slotIndex];
                if (slot == null)
                {
                    reason = "Cassette Slot 데이터가 없습니다. cassette=" + cassetteRole;
                    return false;
                }
                if (slot.SlotNumber != slotIndex)
                {
                    reason = "Cassette Slot 번호가 목록 위치와 일치하지 않습니다. cassette=" + cassetteRole +
                             ", index=" + (slotIndex + 1) +
                             ", slotNumber=" + (slot.SlotNumber + 1);
                    return false;
                }

                bool hasPointer = !string.IsNullOrWhiteSpace(slot.WaferId) ||
                                  !string.IsNullOrWhiteSpace(slot.WaferInstanceId);
                if (!hasPointer)
                {
                    if (slot.HasWafer)
                    {
                        reason = "점유 Slot에 Material pointer가 없습니다. cassette=" + cassetteRole +
                                 ", slot=" + (slot.SlotNumber + 1);
                        return false;
                    }
                    continue;
                }
                if (string.IsNullOrWhiteSpace(slot.WaferId))
                {
                    reason = "Slot Material 표시 ID가 없습니다. cassette=" + cassetteRole +
                             ", slot=" + (slot.SlotNumber + 1);
                    return false;
                }

                string resolveReason = "";
                WaferMaterial wafer = ResolveCassetteSlotWaferForValidationNoLock(slot, out resolveReason);
                if (wafer == null)
                {
                    reason = "Slot Material pointer를 확인할 수 없습니다. cassette=" + cassetteRole +
                             ", slot=" + (slot.SlotNumber + 1) +
                             ", detail=" + resolveReason;
                    return false;
                }
                if (!IsWaferAtCassetteSlot(wafer, cassetteRole, slot.SlotNumber))
                {
                    reason = "Slot pointer와 Wafer 위치가 일치하지 않습니다. cassette=" + cassetteRole +
                             ", slot=" + (slot.SlotNumber + 1) +
                             ", wafer=" + (wafer.WaferId ?? "") +
                             ", location=" + wafer.CurrentLocation;
                    return false;
                }
                WaferMaterialState waferState = WaferMaterialStateText.Normalize(wafer.State);
                if (slot.HasWafer == (waferState == WaferMaterialState.Empty))
                {
                    reason = "Slot 점유 플래그와 Wafer 상태가 일치하지 않습니다. cassette=" + cassetteRole +
                             ", slot=" + (slot.SlotNumber + 1) +
                             ", wafer=" + (wafer.WaferId ?? "") +
                             ", state=" + waferState;
                    return false;
                }
            }

            if (IsOutputCassetteRole(cassetteRole))
            {
                List<DieMaterial> relatedDies;
                string dieReason;
                if (!TryCollectOutputCassetteClearDiesNoLock(
                    GetCassetteClearTargetWafersNoLock(cassetteRole),
                    out relatedDies,
                    out dieReason))
                {
                    reason = dieReason;
                    return false;
                }
            }
            else
            {
                List<DieMaterial> inputOnlyDies;
                string inputReason;
                if (!TryCollectInputLocationClearDiesNoLock(
                    GetCassetteClearTargetWafersNoLock(cassetteRole),
                    MaterialLocationKind.InputCassette,
                    out inputOnlyDies,
                    out inputReason,
                    cassetteRole))
                {
                    reason = inputReason;
                    return false;
                }
            }

            return true;
        }

        private static bool TryValidateInputCassetteWaferPointersNoLock(out string reason)
        {
            reason = "";
            foreach (WaferMaterial wafer in State.Wafers)
            {
                if (wafer == null || wafer.CurrentLocation == null ||
                    wafer.CurrentLocation.Kind != MaterialLocationKind.InputCassette)
                    continue;

                MaterialLocation location = wafer.CurrentLocation;
                List<CassetteMaterial> cassettes = State.Cassettes.Where(c => c != null && c.Role == location.CassetteRole).ToList();
                if ((location.CassetteRole != CassetteMaterialRole.Input1 && location.CassetteRole != CassetteMaterialRole.Input2) ||
                    cassettes.Count != 1 || cassettes[0].Slots == null ||
                    location.SlotNumber < 0 || location.SlotNumber >= cassettes[0].Slots.Count)
                {
                    reason = "입력 Wafer의 카세트 단/슬롯을 확인할 수 없습니다. wafer=" +
                             (wafer.WaferId ?? "") + ", location=" + location;
                    return false;
                }

                string pointerReason;
                WaferMaterial pointedWafer = ResolveCassetteSlotWaferForValidationNoLock(
                    cassettes[0].Slots[location.SlotNumber], out pointerReason);
                if (!ReferenceEquals(pointedWafer, wafer))
                {
                    reason = "입력 카세트 Wafer를 가리키는 슬롯 연결이 일치하지 않습니다. wafer=" +
                             (wafer.WaferId ?? "") + ", location=" + location + ", detail=" + pointerReason;
                    return false;
                }
            }
            return true;
        }

    }
}
