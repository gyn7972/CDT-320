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
    // MaterialStateService partial: 카세트 매핑 갱신 API(원본 1338-1556) + 매핑 검증/적용/세대 보존/슬롯 위치(원본 11779-13641)
    public static partial class MaterialStateService
    {
        public static void UpdateInputCassetteMapping(
            int levelCount,
            int slotCount,
            IReadOnlyList<bool> level1Map,
            IReadOnlyList<bool> level2Map,
            IReadOnlyList<double> level1SlotPositions,
            IReadOnlyList<double> level2SlotPositions,
            string cassetteLotId,
            string tapeFrameSpecName)
        {
            if (levelCount < 1) levelCount = 1;
            if (levelCount > 2) levelCount = 2;
            MaterialCompactionResult compactionResult;

            if (levelCount >= 2 && level2Map == null)
                level2Map = level1Map;

            lock (_stateSync)
            {
                CassetteMaterialRole[] lotRoles = levelCount >= 2
                    ? new[] { CassetteMaterialRole.Input1, CassetteMaterialRole.Input2 }
                    : new[] { CassetteMaterialRole.Input1 };
                string resolvedLotId = ResolveOrCreateCassetteMappingLotId(cassetteLotId, lotRoles);
                ValidateCassetteMappingRequest(CassetteMaterialRole.Input1, true, slotCount, level1Map, level1SlotPositions);
                ValidateCassetteMappingRequest(CassetteMaterialRole.Input2, levelCount >= 2, slotCount, level2Map, level2SlotPositions);
                ValidateMappingIdentityRotationNoLock(CassetteMaterialRole.Input1, true, slotCount, level1Map, true);
                ValidateMappingIdentityRotationNoLock(CassetteMaterialRole.Input2, levelCount >= 2, slotCount, level2Map, true);

                UpdateCassetteMapping(CassetteMaterialRole.Input1, true, slotCount, level1Map, level1SlotPositions, resolvedLotId, tapeFrameSpecName);
                UpdateCassetteMapping(CassetteMaterialRole.Input2, levelCount >= 2, slotCount, level2Map, level2SlotPositions, resolvedLotId, tapeFrameSpecName);
                compactionResult = CompactMaterialStateNoLock();
            }
            LogMaterialCompaction("InputCassetteMapping", compactionResult);
            NotifyAndSave("InputCassetteMapping");
        }

        public static void UpdateOutputCassetteMapping(
            int goodLevelCount,
            int slotCount,
            IReadOnlyList<bool> good1Map,
            IReadOnlyList<bool> good2Map,
            IReadOnlyList<bool> ngMap,
            IReadOnlyList<double> good1SlotPositions,
            IReadOnlyList<double> good2SlotPositions,
            IReadOnlyList<double> ngSlotPositions,
            string cassetteLotId,
            string tapeFrameSpecName)
        {
            if (goodLevelCount < 1) goodLevelCount = 1;
            if (goodLevelCount > 2) goodLevelCount = 2;
            MaterialCompactionResult compactionResult;

            lock (_stateSync)
            {
                CassetteMaterialRole[] lotRoles = goodLevelCount >= 2
                    ? new[] { CassetteMaterialRole.Good1, CassetteMaterialRole.Good2, CassetteMaterialRole.Ng1 }
                    : new[] { CassetteMaterialRole.Good1, CassetteMaterialRole.Ng1 };
                string resolvedLotId = ResolveOrCreateCassetteMappingLotId(cassetteLotId, lotRoles);
                ValidateCassetteMappingRequest(CassetteMaterialRole.Good1, true, slotCount, good1Map, good1SlotPositions);
                ValidateCassetteMappingRequest(CassetteMaterialRole.Good2, goodLevelCount >= 2, slotCount, good2Map, good2SlotPositions);
                ValidateCassetteMappingRequest(CassetteMaterialRole.Ng1, true, slotCount, ngMap, ngSlotPositions);
                ValidateMappingIdentityRotationNoLock(CassetteMaterialRole.Good1, true, slotCount, good1Map, true);
                ValidateMappingIdentityRotationNoLock(CassetteMaterialRole.Good2, goodLevelCount >= 2, slotCount, good2Map, true);
                ValidateMappingIdentityRotationNoLock(CassetteMaterialRole.Ng1, true, slotCount, ngMap, true);
                ValidateOutputMappingDetachCandidatesNoLock(CassetteMaterialRole.Good1);
                ValidateOutputMappingDetachCandidatesNoLock(CassetteMaterialRole.Good2);
                ValidateOutputMappingDetachCandidatesNoLock(CassetteMaterialRole.Ng1);

                UpdateCassetteMapping(CassetteMaterialRole.Good1, true, slotCount, good1Map, good1SlotPositions, resolvedLotId, tapeFrameSpecName);
                UpdateCassetteMapping(CassetteMaterialRole.Good2, goodLevelCount >= 2, slotCount, good2Map, good2SlotPositions, resolvedLotId, tapeFrameSpecName);
                UpdateCassetteMapping(CassetteMaterialRole.Ng1, true, slotCount, ngMap, ngSlotPositions, resolvedLotId, tapeFrameSpecName);
                compactionResult = CompactMaterialStateNoLock();
            }
            LogMaterialCompaction("OutputCassetteMapping", compactionResult);
            NotifyAndSave("OutputCassetteMapping");
        }

        public static void UpdateOutputCassetteMappingSelective(
            bool updateGood,
            bool updateNg,
            int goodLevelCount,
            int slotCount,
            IReadOnlyList<bool> good1Map,
            IReadOnlyList<bool> good2Map,
            IReadOnlyList<bool> ngMap,
            IReadOnlyList<double> good1SlotPositions,
            IReadOnlyList<double> good2SlotPositions,
            IReadOnlyList<double> ngSlotPositions,
            string cassetteLotId,
            string tapeFrameSpecName,
            bool recoverDetachedWorkingMaterial)
        {
            if (goodLevelCount < 1) goodLevelCount = 1;
            if (goodLevelCount > 2) goodLevelCount = 2;
            MaterialCompactionResult compactionResult;

            lock (_stateSync)
            {
                var lotRoles = new List<CassetteMaterialRole>();
                if (updateGood)
                {
                    lotRoles.Add(CassetteMaterialRole.Good1);
                    if (goodLevelCount >= 2)
                        lotRoles.Add(CassetteMaterialRole.Good2);
                }
                if (updateNg)
                    lotRoles.Add(CassetteMaterialRole.Ng1);
                string resolvedLotId = ResolveOrCreateCassetteMappingLotId(cassetteLotId, lotRoles.ToArray());
                if (updateGood)
                {
                    ValidateCassetteMappingRequest(
                        CassetteMaterialRole.Good1,
                        true,
                        slotCount,
                        good1Map,
                        good1SlotPositions,
                        recoverDetachedWorkingMaterial);
                    ValidateCassetteMappingRequest(
                        CassetteMaterialRole.Good2,
                        goodLevelCount >= 2,
                        slotCount,
                        good2Map,
                        good2SlotPositions,
                        recoverDetachedWorkingMaterial);
                    ValidateMappingIdentityRotationNoLock(
                        CassetteMaterialRole.Good1,
                        true,
                        slotCount,
                        good1Map,
                        true,
                        recoverDetachedWorkingMaterial);
                    ValidateMappingIdentityRotationNoLock(
                        CassetteMaterialRole.Good2,
                        goodLevelCount >= 2,
                        slotCount,
                        good2Map,
                        true,
                        recoverDetachedWorkingMaterial);
                }

                if (updateNg)
                {
                    ValidateCassetteMappingRequest(
                        CassetteMaterialRole.Ng1,
                        true,
                        slotCount,
                        ngMap,
                        ngSlotPositions,
                        recoverDetachedWorkingMaterial);
                    ValidateMappingIdentityRotationNoLock(
                        CassetteMaterialRole.Ng1,
                        true,
                        slotCount,
                        ngMap,
                        true,
                        recoverDetachedWorkingMaterial);
                }

                if (updateGood)
                {
                    ValidateOutputMappingDetachCandidatesNoLock(CassetteMaterialRole.Good1);
                    if (goodLevelCount >= 2)
                        ValidateOutputMappingDetachCandidatesNoLock(CassetteMaterialRole.Good2);
                }
                if (updateNg)
                    ValidateOutputMappingDetachCandidatesNoLock(CassetteMaterialRole.Ng1);

                if (recoverDetachedWorkingMaterial)
                {
                    if (updateGood)
                    {
                        RemoveDetachedWorkingOutputMaterialForMapping(CassetteMaterialRole.Good1);
                        if (goodLevelCount >= 2)
                            RemoveDetachedWorkingOutputMaterialForMapping(CassetteMaterialRole.Good2);
                    }

                    if (updateNg)
                        RemoveDetachedWorkingOutputMaterialForMapping(CassetteMaterialRole.Ng1);
                }

                if (updateGood)
                {
                    UpdateCassetteMapping(CassetteMaterialRole.Good1, true, slotCount, good1Map, good1SlotPositions, resolvedLotId, tapeFrameSpecName);
                    UpdateCassetteMapping(CassetteMaterialRole.Good2, goodLevelCount >= 2, slotCount, good2Map, good2SlotPositions, resolvedLotId, tapeFrameSpecName);
                }

                if (updateNg)
                    UpdateCassetteMapping(CassetteMaterialRole.Ng1, true, slotCount, ngMap, ngSlotPositions, resolvedLotId, tapeFrameSpecName);
                compactionResult = CompactMaterialStateNoLock();
            }
            LogMaterialCompaction("OutputCassetteMappingSelective", compactionResult);
            NotifyAndSave("OutputCassetteMappingSelective");
        }

        // To do: [NG 스킵] 카세트 사용 여부를 설정 파라미터와 동기화한다.
        //        IsEnabled=false면 OutputSlotPlanner의 공급/일관성/스토어 판단에서 해당 카세트가 자동 제외된다.
        public static void SetCassetteEnabled(CassetteMaterialRole role, bool enabled)
        {
            bool changed = false;
            lock (_stateSync)
            {
                var cassette = State != null && State.Cassettes != null
                    ? State.Cassettes.FirstOrDefault(c => c != null && c.Role == role)
                    : null;
                if (cassette != null && cassette.IsEnabled != enabled)
                {
                    cassette.IsEnabled = enabled;
                    changed = true;
                }
            }

            if (changed)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Cassette enabled state changed. role=" + role + ", enabled=" + enabled + " - Ok");
                NotifyAndSave("SetCassetteEnabled:" + role);
            }
        }

        private static WaferMaterial ResolveCassetteSlotWaferNoLock(
            CassetteSlotMaterial slot,
            out string reason)
        {
            return ResolveCassetteSlotWaferCoreNoLock(slot, true, true, out reason);
        }

        private static WaferMaterial ResolveCassetteSlotWaferForValidationNoLock(
            CassetteSlotMaterial slot,
            out string reason)
        {
            return ResolveCassetteSlotWaferCoreNoLock(slot, false, false, out reason);
        }

        private static WaferMaterial ResolveCassetteSlotWaferCoreNoLock(
            CassetteSlotMaterial slot,
            bool repairLegacyInstancePointer,
            bool requireOccupied,
            out string reason)
        {
            reason = "";
            if (slot == null)
            {
                reason = "slot 데이터가 없습니다.";
                return null;
            }
            if ((requireOccupied && !slot.HasWafer) || string.IsNullOrWhiteSpace(slot.WaferId))
            {
                reason = "slot이 비어 있습니다.";
                return null;
            }

            string slotInstanceId = (slot.WaferInstanceId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(slotInstanceId))
            {
                List<WaferMaterial> instanceCandidates = State.Wafers
                    .Where(w =>
                        w != null &&
                        string.Equals(
                            w.WaferInstanceId ?? "",
                            slotInstanceId,
                            StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToList();
                if (instanceCandidates.Count != 1)
                {
                    reason = "slot의 WaferInstanceId 대상 Material 수가 1개가 아닙니다. instance=" +
                             slotInstanceId + ", waferId=" + slot.WaferId +
                             ", candidates=" + instanceCandidates.Count;
                    return null;
                }
                WaferMaterial instanceWafer = instanceCandidates[0];
                if (!string.Equals(
                    instanceWafer.WaferId ?? "",
                    slot.WaferId ?? "",
                    StringComparison.OrdinalIgnoreCase))
                {
                    reason = "slot의 WaferId와 WaferInstanceId 대상이 다릅니다. slotWafer=" +
                             slot.WaferId + ", instanceWafer=" + instanceWafer.WaferId +
                             ", instance=" + slotInstanceId;
                    return null;
                }
                return instanceWafer;
            }

            List<WaferMaterial> legacyCandidates = State.Wafers
                .Where(w =>
                    w != null &&
                    string.Equals(
                        w.WaferId ?? "",
                        slot.WaferId ?? "",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (legacyCandidates.Count != 1)
            {
                reason = "V1 slot의 WaferId 대상 Material 수가 1개가 아닙니다. waferId=" +
                         slot.WaferId + ", candidates=" + legacyCandidates.Count;
                return null;
            }

            WaferMaterial legacyWafer = legacyCandidates[0];
            if (repairLegacyInstancePointer)
                slot.WaferInstanceId = EnsureWaferInstanceIdNoLock(legacyWafer);
            return legacyWafer;
        }

        private static void RemoveWaferFromCassetteSlot(WaferMaterial wafer)
        {
            if (wafer == null)
                return;

            string waferId = wafer.WaferId ?? "";
            string waferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
            foreach (var cassette in State.Cassettes)
            {
                foreach (var slot in cassette.Slots)
                {
                    if (slot == null)
                        continue;

                    bool sameInstance;
                    if (!string.IsNullOrWhiteSpace(slot.WaferInstanceId))
                    {
                        sameInstance = string.Equals(
                            slot.WaferInstanceId,
                            waferInstanceId,
                            StringComparison.OrdinalIgnoreCase);
                    }
                    else if (string.Equals(
                        slot.WaferId ?? "",
                        waferId,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        string slotReason;
                        WaferMaterial slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                        sameInstance = slotWafer != null &&
                                       string.Equals(
                                           EnsureWaferInstanceIdNoLock(slotWafer),
                                           waferInstanceId,
                                           StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        sameInstance = false;
                    }
                    if (sameInstance)
                    {
                        slot.WaferId = "";
                        slot.WaferInstanceId = "";
                        slot.HasWafer = false;
                    }
                }
            }
        }

        private static WaferMaterial FindOtherWaferAtLocation(
            WaferMaterial movingWafer,
            MaterialLocation targetLocation)
        {
            if (targetLocation == null || targetLocation.Kind == MaterialLocationKind.Unknown)
                return null;

            string movingInstanceId = EnsureWaferInstanceIdNoLock(movingWafer);
            return State.Wafers.FirstOrDefault(w =>
                w != null &&
                !ReferenceEquals(w, movingWafer) &&
                !string.Equals(
                    EnsureWaferInstanceIdNoLock(w),
                    movingInstanceId,
                    StringComparison.OrdinalIgnoreCase) &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty &&
                IsSameMaterialLocation(w.CurrentLocation, targetLocation));
        }

        private static bool IsSameMaterialLocation(MaterialLocation left, MaterialLocation right)
        {
            if (left == null || right == null || left.Kind != right.Kind)
                return false;

            if (left.Kind == MaterialLocationKind.InputCassette || left.Kind == MaterialLocationKind.OutputCassette)
                return left.CassetteRole == right.CassetteRole && left.SlotNumber == right.SlotNumber;

            if (left.Kind == MaterialLocationKind.PickerFront || left.Kind == MaterialLocationKind.PickerRear)
                return left.PickerNo == right.PickerNo;

            return true;
        }

        private static void ValidateCassetteMappingRequest(
            CassetteMaterialRole role,
            bool enabled,
            int slotCount,
            IReadOnlyList<bool> map,
            IReadOnlyList<double> slotPositions,
            bool recoverDetachedWorkingMaterial = false)
        {
            if (slotCount <= 0)
                throw new InvalidOperationException("Cassette SlotCount가 유효하지 않습니다. cassette=" + role + ", slotCount=" + slotCount);

            if (!enabled)
            {
                WaferMaterial disabledRoleWafer = State.Wafers.FirstOrDefault(w =>
                    w != null &&
                    w.SourceCassetteRole == role &&
                    WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty);
                if (disabledRoleWafer != null)
                {
                    throw new InvalidOperationException("진행 또는 보관 중인 자재가 있어 cassette role을 비활성화할 수 없습니다. cassette=" + role +
                                                        ", wafer=" + disabledRoleWafer.WaferId +
                                                        ", state=" + disabledRoleWafer.State +
                                                        ", location=" + disabledRoleWafer.CurrentLocation);
                }
                return;
            }

            if (map == null || map.Count != slotCount)
                throw new InvalidOperationException("Cassette mapping 결과 길이가 SlotCount와 다릅니다. cassette=" + role +
                                                    ", slotCount=" + slotCount +
                                                    ", mapCount=" + (map != null ? map.Count : 0));

            if (slotPositions == null || slotPositions.Count != slotCount)
                throw new InvalidOperationException("Cassette slot 위치 수가 SlotCount와 다릅니다. cassette=" + role +
                                                    ", slotCount=" + slotCount +
                                                    ", positionCount=" + (slotPositions != null ? slotPositions.Count : 0));

            for (int i = 0; i < slotPositions.Count; i++)
            {
                if (double.IsNaN(slotPositions[i]) || double.IsInfinity(slotPositions[i]))
                    throw new InvalidOperationException("Cassette slot 물리 위치가 유효하지 않습니다. cassette=" + role +
                                                        ", slot=" + (i + 1) + ", position=" + slotPositions[i]);
            }

            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c.Role == role);
            if (cassette != null && cassette.Slots != null)
            {
                for (int i = 0; i < cassette.Slots.Count; i++)
                {
                    CassetteSlotMaterial slot = cassette.Slots[i];
                    if (slot == null)
                        throw new InvalidOperationException("기존 cassette slot 데이터가 없습니다. cassette=" + role + ", slot=" + (i + 1));

                    bool hasWaferId = !string.IsNullOrWhiteSpace(slot.WaferId);
                    bool hasWaferInstanceId = !string.IsNullOrWhiteSpace(slot.WaferInstanceId);
                    bool isStateOnlyEmptyPointer = false;
                    if (!slot.HasWafer && hasWaferId && hasWaferInstanceId)
                    {
                        string emptyPointerReason;
                        WaferMaterial emptyPointerWafer = ResolveCassetteSlotWaferForValidationNoLock(
                            slot,
                            out emptyPointerReason);
                        isStateOnlyEmptyPointer = emptyPointerWafer != null &&
                            WaferMaterialStateText.Normalize(emptyPointerWafer.State) == WaferMaterialState.Empty &&
                            IsWaferAtCassetteSlot(emptyPointerWafer, role, i);
                    }

                    if ((!slot.HasWafer && (hasWaferId || hasWaferInstanceId) && !isStateOnlyEmptyPointer) ||
                        (slot.HasWafer && (!hasWaferId || !hasWaferInstanceId)))
                    {
                        throw new InvalidOperationException("기존 cassette slot 점유 상태와 Material pointer가 불일치합니다. cassette=" + role +
                                                            ", slot=" + (i + 1) +
                                                            ", hasWafer=" + slot.HasWafer +
                                                            ", waferId=" + slot.WaferId +
                                                            ", instance=" + slot.WaferInstanceId);
                    }

                    if (!slot.HasWafer)
                        continue;

                    string slotReason;
                    WaferMaterial slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                    if (slotWafer == null)
                    {
                        throw new InvalidOperationException("기존 cassette 점유 slot의 Material 데이터가 없습니다. cassette=" + role +
                                                            ", slot=" + (i + 1) +
                                                            ", waferId=" + slot.WaferId +
                                                            ", reason=" + slotReason);
                    }

                    WaferMaterialState slotWaferState = WaferMaterialStateText.Normalize(slotWafer.State);
                    if (slotWaferState == WaferMaterialState.Empty ||
                        !IsWaferAtCassetteSlot(slotWafer, role, i) ||
                        slotWafer.SourceCassetteRole != role ||
                        slotWafer.SourceSlotNumber != i)
                    {
                        throw new InvalidOperationException("기존 cassette slot과 Material 위치/source 정보가 불일치합니다. cassette=" + role +
                                                            ", slot=" + (i + 1) +
                                                            ", waferId=" + slotWafer.WaferId +
                                                            ", state=" + slotWaferState +
                                                            ", sourceRole=" + slotWafer.SourceCassetteRole +
                                                            ", sourceSlot=" + (slotWafer.SourceSlotNumber + 1) +
                                                            ", location=" + slotWafer.CurrentLocation);
                    }
                }
            }

            MaterialLocationKind cassetteLocation = IsOutputCassetteRole(role)
                ? MaterialLocationKind.OutputCassette
                : MaterialLocationKind.InputCassette;
            foreach (WaferMaterial wafer in State.Wafers)
            {
                if (wafer == null || wafer.SourceCassetteRole != role)
                    continue;

                WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
                if (state == WaferMaterialState.Empty)
                    continue;

                MaterialLocation location = wafer.CurrentLocation;
                bool atSourceCassette = location != null &&
                                        location.Kind == cassetteLocation &&
                                        location.CassetteRole == role &&
                                        location.SlotNumber == wafer.SourceSlotNumber;
                if (!atSourceCassette)
                {
                    if (recoverDetachedWorkingMaterial &&
                        IsDetachedWorkingOutputMaterial(wafer, role))
                    {
                        continue;
                    }

                    throw new InvalidOperationException("공정 중 자재가 cassette 밖에 있어 재매핑할 수 없습니다. cassette=" + role +
                                                        ", wafer=" + wafer.WaferId +
                                                        ", sourceSlot=" + (wafer.SourceSlotNumber + 1) +
                                                        ", state=" + state +
                                                        ", location=" + location);
                }

                if (wafer.SourceSlotNumber < 0 || wafer.SourceSlotNumber >= slotCount)
                {
                    throw new InvalidOperationException("기존 자재의 source slot이 새 SlotCount 범위를 벗어납니다. cassette=" + role +
                                                        ", wafer=" + wafer.WaferId +
                                                        ", sourceSlot=" + (wafer.SourceSlotNumber + 1) +
                                                        ", slotCount=" + slotCount);
                }

                if (!map[wafer.SourceSlotNumber] && state != WaferMaterialState.Finish)
                {
                    throw new InvalidOperationException("Mapping 센서는 빈 slot이지만 처리 전 자재 데이터가 남아 있습니다. cassette=" + role +
                                                        ", slot=" + (wafer.SourceSlotNumber + 1) +
                                                        ", wafer=" + wafer.WaferId +
                                                        ", state=" + state);
                }
            }
        }

        private static string ResolveOrCreateCassetteMappingLotId(
            string requestedLotId,
            params CassetteMaterialRole[] roles)
        {
            var candidates = new List<string>();
            if (State != null)
            {
                candidates.Add(State.LotId);
                var roleSet = new HashSet<CassetteMaterialRole>(roles ?? new CassetteMaterialRole[0]);
                foreach (CassetteMaterial cassette in State.Cassettes.Where(c => c != null && roleSet.Contains(c.Role)))
                {
                    // LOT 충돌 검사가 보호하는 대상은 카세트 안 실물 자재다. 자재가 하나도 없는
                    // 카세트에 남은 LOT ID는 잔존 문자열일 뿐이므로 후보에서 제외한다.
                    if (CassetteHasAnySlotMaterial(cassette))
                        candidates.Add(cassette.CassetteLotId);
                    else if (!string.IsNullOrWhiteSpace(cassette.CassetteLotId))
                        Log.Write("Main", "SYSTEM", "MaterialLotContext",
                            "자재가 없는 카세트의 잔존 LOT ID를 후보에서 제외합니다. cassette=" + cassette.Role +
                            ", staleLotId=" + cassette.CassetteLotId + " - Check");

                    if (cassette.Slots == null)
                        continue;

                    foreach (CassetteSlotMaterial slot in cassette.Slots)
                    {
                        if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                            continue;
                        string slotReason;
                        WaferMaterial wafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                        if (wafer == null)
                        {
                            throw new InvalidOperationException(
                                "Cassette LOT 결정 중 slot의 물리 Material pointer가 올바르지 않습니다. cassette=" +
                                cassette.Role +
                                ", slot=" + (slot.SlotNumber + 1) +
                                ", waferId=" + slot.WaferId +
                                ", reason=" + slotReason);
                        }
                        if (wafer != null && !IsFinishedOutputBinWafer(cassette.Role, wafer))
                            candidates.Add(wafer.CassetteLotId);
                    }
                }
            }

            return ResolveOrCreateCassetteLotId(requestedLotId, candidates, "cassette mapping");
        }

        private static string ResolveOrCreateCassetteLotId(
            string requestedLotId,
            CassetteMaterial cassette,
            WaferMaterial wafer)
        {
            var candidates = new List<string>();
            if (wafer != null)
                candidates.Add(wafer.CassetteLotId);
            if (cassette != null)
            {
                // 매핑 후보 산정과 같은 규칙: 자재 없는 카세트의 잔존 LOT ID는 후보에서 제외.
                if (CassetteHasAnySlotMaterial(cassette))
                    candidates.Add(cassette.CassetteLotId);
                else if (!string.IsNullOrWhiteSpace(cassette.CassetteLotId))
                    Log.Write("Main", "SYSTEM", "MaterialLotContext",
                        "자재가 없는 카세트의 잔존 LOT ID를 후보에서 제외합니다. cassette=" + cassette.Role +
                        ", staleLotId=" + cassette.CassetteLotId + " - Check");
            }
            if (State != null)
                candidates.Add(State.LotId);

            return ResolveOrCreateCassetteLotId(requestedLotId, candidates, "cassette wafer");
        }

        private static bool CassetteHasAnySlotMaterial(CassetteMaterial cassette)
        {
            return cassette != null &&
                   cassette.Slots != null &&
                   cassette.Slots.Any(s => s != null && s.HasWafer && !string.IsNullOrWhiteSpace(s.WaferId));
        }

        private static string ResolveOrCreateCassetteLotId(
            string requestedLotId,
            IEnumerable<string> existingLotIds,
            string context)
        {
            var candidates = new List<string> { requestedLotId };
            candidates.AddRange(existingLotIds ?? Enumerable.Empty<string>());
            List<string> existing = candidates
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (existing.Count == 1)
                return existing[0];
            if (existing.Count > 1)
            {
                throw new InvalidOperationException(
                    "카세트 LOT ID 후보가 서로 달라 LOT ID를 결정할 수 없습니다. context=" + context +
                    ", requestedLotId=" + (requestedLotId ?? "") +
                    ", lotIds=" + string.Join(",", existing.ToArray()));
            }

            string generatedLotId = "AUTOLOT-" +
                                    DateTime.Now.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture) + "-" +
                                    Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            Log.Write("Main", "SYSTEM", "MaterialLotContext",
                "카세트 LOT ID가 없어 임시 LOT ID를 생성했습니다. lotId=" + generatedLotId + " - Check");
            return generatedLotId;
        }

        private static List<WaferMaterial> GetCassetteSlotCountReductionTargetsNoLock(
            CassetteMaterialRole role,
            int newSlotCount)
        {
            if (newSlotCount < 0)
                throw new ArgumentOutOfRangeException(nameof(newSlotCount));

            var targets = new HashSet<WaferMaterial>();
            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == role);
            if (cassette != null && cassette.Slots != null)
            {
                for (int slotIndex = newSlotCount; slotIndex < cassette.Slots.Count; slotIndex++)
                {
                    CassetteSlotMaterial slot = cassette.Slots[slotIndex];
                    if (slot == null)
                        continue;

                    bool hasPointer = !string.IsNullOrWhiteSpace(slot.WaferId) ||
                                      !string.IsNullOrWhiteSpace(slot.WaferInstanceId);
                    if (!hasPointer)
                        continue;

                    string resolveReason;
                    WaferMaterial wafer = ResolveCassetteSlotWaferForValidationNoLock(slot, out resolveReason);
                    if (wafer == null)
                    {
                        throw new InvalidOperationException(
                            "축소 대상 Cassette Slot Material pointer를 확인할 수 없습니다. cassette=" +
                            role + ", slot=" + (slotIndex + 1) + ", detail=" + resolveReason);
                    }
                    targets.Add(wafer);
                }
            }

            MaterialLocationKind expectedKind = IsOutputCassetteRole(role)
                ? MaterialLocationKind.OutputCassette
                : MaterialLocationKind.InputCassette;
            foreach (WaferMaterial wafer in State.Wafers)
            {
                if (wafer != null &&
                    wafer.CurrentLocation != null &&
                    wafer.CurrentLocation.Kind == expectedKind &&
                    wafer.CurrentLocation.CassetteRole == role &&
                    wafer.CurrentLocation.SlotNumber >= newSlotCount)
                {
                    targets.Add(wafer);
                }
            }

            return targets.ToList();
        }

        private static void ValidateCassetteSlotCountReductionNoLock(
            CassetteMaterialRole role,
            int newSlotCount)
        {
            List<WaferMaterial> targets = GetCassetteSlotCountReductionTargetsNoLock(role, newSlotCount);
            if (targets.Count == 0)
                return;

            string roleReason;
            if (!TryValidateCassetteRoleForClearNoLock(role, out roleReason))
            {
                throw new InvalidOperationException(
                    "Cassette Slot 축소 전 Material 검증에 실패했습니다. cassette=" +
                    role + ", detail=" + roleReason);
            }

            string dieReason;
            if (IsOutputCassetteRole(role))
            {
                List<DieMaterial> outputDies;
                if (!TryCollectOutputCassetteClearDiesNoLock(targets, out outputDies, out dieReason))
                {
                    throw new InvalidOperationException(
                        "Cassette Slot 축소 대상 Output Material을 안전하게 정리할 수 없습니다. cassette=" +
                        role + ", detail=" + dieReason);
                }
                return;
            }

            List<DieMaterial> inputOnlyDies;
            if (!TryCollectInputLocationClearDiesNoLock(
                targets,
                MaterialLocationKind.InputCassette,
                out inputOnlyDies,
                out dieReason))
            {
                throw new InvalidOperationException(
                    "Cassette Slot 축소 대상 Input Material을 안전하게 정리할 수 없습니다. cassette=" +
                    role + ", detail=" + dieReason);
            }
        }

        private static void ApplyCassetteSlotCountReductionNoLock(
            CassetteMaterialRole role,
            int newSlotCount)
        {
            List<WaferMaterial> targets = GetCassetteSlotCountReductionTargetsNoLock(role, newSlotCount);
            if (targets.Count == 0)
                return;

            ValidateCassetteSlotCountReductionNoLock(role, newSlotCount);
            if (IsOutputCassetteRole(role))
            {
                List<DieMaterial> outputDies;
                string outputReason;
                if (!TryCollectOutputCassetteClearDiesNoLock(targets, out outputDies, out outputReason))
                    throw new InvalidOperationException(outputReason);
                DetachOrRemoveClearedOutputDiesNoLock(outputDies);
                foreach (WaferMaterial wafer in targets)
                    ClearOutputStageWaferProcessingFieldsNoLock(wafer);
            }
            else
            {
                List<DieMaterial> historyDies;
                bool preserveHistory;
                string inputReason;
                if (!TryCollectInputHistoryForClearNoLock(
                    targets,
                    out historyDies,
                    out preserveHistory,
                    out inputReason))
                {
                    throw new InvalidOperationException(inputReason);
                }

                if (preserveHistory)
                    DemotePreservedInputHistoryLocationsNoLock(historyDies, MaterialLocationKind.InputCassette);
                else
                    RemoveDieMaterialsNoLock(historyDies);
            }

            DateTime updatedAt = DateTime.Now;
            foreach (WaferMaterial wafer in targets)
            {
                wafer.State = WaferMaterialState.Empty;
                wafer.CurrentLocation = MaterialLocation.Unknown();
                wafer.CassetteLotId = "";
                wafer.UpdatedAt = updatedAt;
            }

            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == role);
            if (cassette == null || cassette.Slots == null)
                return;
            for (int slotIndex = newSlotCount; slotIndex < cassette.Slots.Count; slotIndex++)
            {
                CassetteSlotMaterial slot = cassette.Slots[slotIndex];
                if (slot == null)
                    continue;
                slot.WaferId = "";
                slot.WaferInstanceId = "";
                slot.HasWafer = false;
            }
        }

        private static void UpdateCassetteMapping(
            CassetteMaterialRole role,
            bool enabled,
            int slotCount,
            IReadOnlyList<bool> map,
            IReadOnlyList<double> slotPositions,
            string cassetteLotId,
            string tapeFrameSpecName,
            bool preserveExistingMaterial = true)
        {
            ValidateOutputMappingDetachCandidatesNoLock(role);
            ValidateMappingIdentityRotationNoLock(
                role,
                enabled,
                slotCount,
                map,
                preserveExistingMaterial);

            ApplyCassetteSlotCountReductionNoLock(role, slotCount);

            var cassette = EnsureCassette(role, slotCount);
            string resolvedLotId = ResolveOrCreateCassetteMappingLotId(cassetteLotId, role);
            cassette.IsEnabled = enabled;
            cassette.IsPresent = enabled;
            cassette.IsMapped = enabled && map != null;
            cassette.CassetteLotId = resolvedLotId;
            cassette.LastScanTime = enabled && map != null ? DateTime.Now : cassette.LastScanTime;
            cassette.SlotCount = slotCount;
            cassette.EnsureSlots();

            for (int i = 0; i < cassette.Slots.Count; i++)
            {
                CassetteSlotMaterial slot = cassette.Slots[i];
                bool mappedOccupied = enabled && map != null && i < map.Count && map[i];
                if (mappedOccupied)
                    continue;

                // 빈 slot으로 다시 매핑할 때 cassette 위치에 남은 Material도 함께 정리한다.
                // Stage/Feeder 등 cassette 밖으로 이동한 Material은 이 조건에 포함하지 않는다.
                List<WaferMaterial> removedWafers = State.Wafers
                    .Where(w => w != null && IsWaferAtCassetteSlot(w, role, i))
                    .ToList();
                foreach (WaferMaterial removedWafer in removedWafers)
                {
                    if (IsFinishedOutputBinWafer(role, removedWafer))
                    {
                        RemoveFinishedOutputBinWaferForNewCassetteMapping(removedWafer);
                    }
                    else
                    {
                        if (IsOutputCassetteRole(role))
                            DetachOutputWaferMaterialForReplacementNoLock(removedWafer);
                        removedWafer.CurrentLocation = MaterialLocation.Unknown();
                        removedWafer.State = WaferMaterialState.Empty;
                        removedWafer.UpdatedAt = DateTime.Now;
                    }
                }

                if (slot != null)
                {
                    slot.WaferId = "";
                    slot.WaferInstanceId = "";
                    slot.HasWafer = false;
                }
            }

            if (!enabled || map == null)
                return;

            for (int i = 0; i < slotCount; i++)
            {
                if (!map[i])
                    continue;

                CassetteSlotMaterial slot = cassette.Slots[i];
                string slotResolveReason = "";
                bool slotHasPointer = slot != null &&
                                      (!string.IsNullOrWhiteSpace(slot.WaferId) ||
                                       !string.IsNullOrWhiteSpace(slot.WaferInstanceId));
                WaferMaterial wafer = slotHasPointer
                    ? ResolveCassetteSlotWaferNoLock(slot, out slotResolveReason)
                    : null;
                if (preserveExistingMaterial &&
                    slotHasPointer &&
                    wafer == null)
                {
                    throw new InvalidOperationException(
                        "보존 Cassette Mapping의 slot Material pointer가 올바르지 않습니다. role=" +
                        role + ", slot=" + (i + 1) +
                        ", wafer=" + slot.WaferId +
                        ", reason=" + slotResolveReason);
                }
                bool stateOnlyEmptyMarker = IsStateOnlyEmptyCassetteMaterialNoLock(wafer, role, i);
                bool preserveExisting = preserveExistingMaterial &&
                                        wafer != null &&
                                        IsWaferAtCassetteSlot(wafer, role, i) &&
                                        (WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty ||
                                         stateOnlyEmptyMarker) &&
                                        !IsFinishedOutputBinWafer(role, wafer);

                if (IsFinishedOutputBinWafer(role, wafer))
                {
                    RemoveFinishedOutputBinWaferForNewCassetteMapping(wafer);
                    wafer = null;
                }

                if (!preserveExisting)
                {
                    string waferId = BuildGeneratedWaferId(role, i);
                    List<WaferMaterial> staleSlotWafers = State.Wafers
                        .Where(w =>
                            w != null &&
                            !string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase) &&
                            IsWaferAtCassetteSlot(w, role, i))
                        .ToList();
                    foreach (WaferMaterial staleWafer in staleSlotWafers)
                    {
                        if (role == CassetteMaterialRole.Input1 || role == CassetteMaterialRole.Input2)
                        {
                            ResetInputStageWaferProcessingStateNoLock(
                                staleWafer,
                                "ProcessTestMapping.ReplaceExistingWafer");
                        }
                        else if (IsOutputCassetteRole(role))
                        {
                            DetachOutputWaferMaterialForReplacementNoLock(staleWafer);
                        }

                        staleWafer.CurrentLocation = MaterialLocation.Unknown();
                        staleWafer.State = WaferMaterialState.Empty;
                        staleWafer.UpdatedAt = DateTime.Now;
                    }

                    List<WaferMaterial> reusableCandidates = State.Wafers
                        .Where(w =>
                            w != null &&
                            string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    var rotatableCandidates = new List<WaferMaterial>();
                    foreach (WaferMaterial candidate in reusableCandidates)
                    {
                        if (IsOutputCassetteRole(role) &&
                            IsStateOnlyEmptyCassetteMaterialNoLock(candidate, role, i))
                            continue;

                        string identityBlockReason;
                        if (TryGetWaferIdentityRotationBlockerNoLock(candidate, out identityBlockReason))
                        {
                            string preserveReason;
                            if ((role == CassetteMaterialRole.Input1 || role == CassetteMaterialRole.Input2) &&
                                CanPreserveInputWaferGenerationForMappingNoLock(candidate, out preserveReason))
                            {
                                // Output 제품/receive 계획이 아직 이전 Input instance를 참조한다.
                                // 과거 세대는 그대로 두고 아래에서 같은 표시 ID의 새 세대를 만든다.
                                continue;
                            }

                            throw new InvalidOperationException(
                                "새 physical wafer Mapping이 이전 Material identity 참조와 충돌합니다. role=" +
                                role + ", slot=" + (i + 1) +
                                ", wafer=" + waferId +
                                ", detail=" + identityBlockReason);
                        }

                        rotatableCandidates.Add(candidate);
                    }

                    if (rotatableCandidates.Count > 1)
                    {
                        throw new InvalidOperationException(
                            "새 physical wafer Mapping에 재사용 가능한 이전 세대가 둘 이상입니다. role=" +
                            role + ", slot=" + (i + 1) +
                            ", wafer=" + waferId +
                            ", candidates=" + rotatableCandidates.Count);
                    }

                    wafer = rotatableCandidates.FirstOrDefault();
                    if (wafer == null)
                    {
                        wafer = new WaferMaterial
                        {
                            WaferId = waferId,
                            CreatedAt = DateTime.Now
                        };
                        State.Wafers.Add(wafer);
                    }

                    if (IsOutputCassetteRole(role) && wafer != null)
                        DetachOutputWaferMaterialForReplacementNoLock(wafer);

                    // Slot 고정 WaferId 재사용 시 새 physical wafer가 이전 Align/Mapping/Review 승인을
                    // 상속하지 않도록 비보존 매핑 경로에서는 항상 처리 상태를 초기화한다.
                    ResetInputStageWaferProcessingStateNoLock(wafer, "CassetteMapping.NewWafer");

                    wafer.CassetteLotId = resolvedLotId;
                    wafer.SourceCassetteId = cassette.CassetteId;
                    wafer.SourceCassetteRole = role;
                    wafer.SourceSlotNumber = i;
                    ApplyWaferCassettePosition(wafer, ResolveSlotPosition(slotPositions, i));
                    wafer.CurrentLocation = MaterialLocation.Cassette(
                        role == CassetteMaterialRole.Input1 || role == CassetteMaterialRole.Input2
                            ? MaterialLocationKind.InputCassette
                            : MaterialLocationKind.OutputCassette,
                        role,
                        i);
                    wafer.State = WaferMaterialState.Ready;
                    wafer.TapeFrameSpecName = tapeFrameSpecName ?? "";
                }
                else
                {
                    wafer.CurrentCassetteSlotPosition = ResolveSlotPosition(slotPositions, i);
                    if (stateOnlyEmptyMarker)
                        wafer.State = WaferMaterialState.Ready;
                }
                wafer.CassetteLotId = resolvedLotId;
                if (IsOutputCassetteRole(role))
                {
                    wafer.OutputCassetteId = cassette.CassetteId;
                    wafer.OutputCassetteRole = role;
                    wafer.OutputSlotNumber = i;
                }
                wafer.UpdatedAt = DateTime.Now;

                slot.WaferId = wafer.WaferId;
                slot.WaferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
                slot.HasWafer = true;
            }
        }

        private static void ValidateMappingIdentityRotationNoLock(
            CassetteMaterialRole role,
            bool enabled,
            int slotCount,
            IReadOnlyList<bool> map,
            bool preserveExistingMaterial,
            bool ignoreDetachedWorkingMaterial = false,
            bool ignoreProcessTestStageMaterial = false)
        {
            ValidateCassetteSlotCountReductionNoLock(role, slotCount);
            if (!enabled || map == null)
                return;

            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == role);
            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                if (slotIndex >= map.Count || !map[slotIndex])
                    continue;

                CassetteSlotMaterial slot = cassette != null && cassette.Slots != null && slotIndex < cassette.Slots.Count
                    ? cassette.Slots[slotIndex]
                    : null;
                string resolveReason = "";
                bool slotHasPointer = slot != null &&
                                      (!string.IsNullOrWhiteSpace(slot.WaferId) ||
                                       !string.IsNullOrWhiteSpace(slot.WaferInstanceId));
                WaferMaterial currentWafer = slotHasPointer
                    ? ResolveCassetteSlotWaferForValidationNoLock(slot, out resolveReason)
                    : null;
                if (slotHasPointer && currentWafer == null)
                {
                    throw new InvalidOperationException(
                        "Cassette Mapping의 기존 slot Material pointer가 올바르지 않습니다. role=" +
                        role + ", slot=" + (slotIndex + 1) +
                        ", reason=" + resolveReason);
                }
                bool stateOnlyEmptyMarker = IsStateOnlyEmptyCassetteMaterialNoLock(
                    currentWafer,
                    role,
                    slotIndex);
                bool preserveExisting = preserveExistingMaterial &&
                                        currentWafer != null &&
                                        IsWaferAtCassetteSlot(currentWafer, role, slotIndex) &&
                                        (WaferMaterialStateText.Normalize(currentWafer.State) != WaferMaterialState.Empty ||
                                         stateOnlyEmptyMarker) &&
                                        !IsFinishedOutputBinWafer(role, currentWafer);
                if (preserveExisting || IsFinishedOutputBinWafer(role, currentWafer))
                    continue;

                string waferId = BuildGeneratedWaferId(role, slotIndex);
                List<WaferMaterial> rotationCandidates = State.Wafers.Where(wafer =>
                        wafer != null &&
                        (string.Equals(wafer.WaferId, waferId, StringComparison.OrdinalIgnoreCase) ||
                         IsWaferAtCassetteSlot(wafer, role, slotIndex)))
                    .ToList();
                if (currentWafer != null && !rotationCandidates.Contains(currentWafer))
                    rotationCandidates.Add(currentWafer);

                int rotatableCandidateCount = 0;
                foreach (WaferMaterial candidate in rotationCandidates.Distinct())
                {
                    if (IsOutputCassetteRole(role) &&
                        IsStateOnlyEmptyCassetteMaterialNoLock(candidate, role, slotIndex))
                        continue;

                    if (ignoreProcessTestStageMaterial &&
                        candidate.CurrentLocation != null &&
                        (candidate.CurrentLocation.Kind == MaterialLocationKind.InputStage ||
                         candidate.CurrentLocation.Kind == MaterialLocationKind.OutputStageGood ||
                         candidate.CurrentLocation.Kind == MaterialLocationKind.OutputStageNg))
                    {
                        continue;
                    }

                    if (ignoreDetachedWorkingMaterial &&
                        IsDetachedWorkingOutputMaterial(candidate, role))
                    {
                        continue;
                    }

                    string identityBlockReason;
                    if (TryGetWaferIdentityRotationBlockerNoLock(candidate, out identityBlockReason))
                    {
                        string preserveReason;
                        if ((role == CassetteMaterialRole.Input1 || role == CassetteMaterialRole.Input2) &&
                            CanPreserveInputWaferGenerationForMappingNoLock(candidate, out preserveReason))
                        {
                            continue;
                        }

                        throw new InvalidOperationException(
                            "새 physical wafer Mapping이 이전 Material identity 참조와 충돌합니다. role=" +
                            role + ", slot=" + (slotIndex + 1) +
                            ", wafer=" + waferId +
                                 ", detail=" + identityBlockReason);
                    }

                    rotatableCandidateCount++;
                }

                if (rotatableCandidateCount > 1)
                {
                    throw new InvalidOperationException(
                        "새 physical wafer Mapping에 재사용 가능한 이전 세대가 둘 이상입니다. role=" +
                        role + ", slot=" + (slotIndex + 1) +
                        ", wafer=" + waferId +
                        ", candidates=" + rotatableCandidateCount);
                }
            }
        }

        private static bool IsStateOnlyEmptyCassetteMaterialNoLock(
            WaferMaterial wafer,
            CassetteMaterialRole role,
            int slotNumber)
        {
            if (wafer == null ||
                WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty ||
                !IsWaferAtCassetteSlot(wafer, role, slotNumber))
            {
                return false;
            }

            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == role);
            CassetteSlotMaterial slot = cassette != null && cassette.Slots != null &&
                                          slotNumber >= 0 && slotNumber < cassette.Slots.Count
                ? cassette.Slots[slotNumber]
                : null;
            if (slot == null || slot.HasWafer)
                return false;

            string waferInstanceId = (wafer.WaferInstanceId ?? "").Trim();
            return !string.IsNullOrWhiteSpace(waferInstanceId) &&
                   string.Equals(slot.WaferInstanceId ?? "", waferInstanceId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(slot.WaferId ?? "", wafer.WaferId ?? "", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// identity 회전 후에도 남게 될 Picker/Output 참조가 있는지 확인한다.
        /// </summary>
        private static bool TryGetWaferIdentityRotationBlockerNoLock(
            WaferMaterial wafer,
            out string reason)
        {
            reason = "";
            if (wafer == null)
                return false;

            string instanceId = (wafer.WaferInstanceId ?? "").Trim();
            string displayId = (wafer.WaferId ?? "").Trim();
            bool displayIdIsUnique = !string.IsNullOrWhiteSpace(displayId) &&
                State.Wafers.Count(candidate =>
                    candidate != null &&
                    string.Equals(candidate.WaferId ?? "", displayId, StringComparison.OrdinalIgnoreCase)) == 1;

            foreach (DieMaterial die in State.Dies)
            {
                if (die == null)
                    continue;

                bool sameInput = !string.IsNullOrWhiteSpace(die.InputWaferInstanceId)
                    ? !string.IsNullOrWhiteSpace(instanceId) &&
                      string.Equals(
                          (die.InputWaferInstanceId ?? "").Trim(),
                          instanceId,
                          StringComparison.OrdinalIgnoreCase)
                    : displayIdIsUnique &&
                      string.Equals(die.WaferID_Input ?? "", displayId, StringComparison.OrdinalIgnoreCase);
                bool sameOutput = !string.IsNullOrWhiteSpace(die.OutputWaferInstanceId)
                    ? !string.IsNullOrWhiteSpace(instanceId) &&
                      string.Equals(
                          (die.OutputWaferInstanceId ?? "").Trim(),
                          instanceId,
                          StringComparison.OrdinalIgnoreCase)
                    : displayIdIsUnique &&
                      !string.IsNullOrWhiteSpace(die.WaferID_Output) &&
                      string.Equals(die.WaferID_Output, displayId, StringComparison.OrdinalIgnoreCase);

                if (sameOutput ||
                    (sameInput && !IsInputOnlyDieForWaferInstanceNoLock(die, displayId, instanceId)))
                {
                    reason = "이전 instance를 참조하는 활성 Die가 있습니다. die=" +
                             (die.DieId ?? "") +
                             ", location=" +
                             (die.CurrentLocation != null ? die.CurrentLocation.Kind.ToString() : "Unknown");
                    return true;
                }
            }

            foreach (WaferMaterial outputWafer in State.Wafers)
            {
                if (outputWafer == null || ReferenceEquals(outputWafer, wafer))
                    continue;

                bool sameSource = !string.IsNullOrWhiteSpace(outputWafer.OutputReceiveSourceWaferInstanceId)
                    ? !string.IsNullOrWhiteSpace(instanceId) &&
                      string.Equals(
                          (outputWafer.OutputReceiveSourceWaferInstanceId ?? "").Trim(),
                          instanceId,
                          StringComparison.OrdinalIgnoreCase)
                    : displayIdIsUnique &&
                      !string.IsNullOrWhiteSpace(outputWafer.OutputReceiveSourceWaferId) &&
                      string.Equals(
                          outputWafer.OutputReceiveSourceWaferId,
                          displayId,
                          StringComparison.OrdinalIgnoreCase);
                if (sameSource)
                {
                    reason = "Output receive source가 이전 instance를 참조합니다. outputWafer=" +
                             (outputWafer.WaferId ?? "");
                    return true;
                }
            }

            if (wafer.OutputReceiveTotalCount > 0 ||
                !string.IsNullOrWhiteSpace(wafer.OutputReceiveSourceWaferInstanceId) ||
                !string.IsNullOrWhiteSpace(wafer.OutputReceiveSourceWaferId) ||
                (wafer.OutputReceiveSlots != null && wafer.OutputReceiveSlots.Count > 0))
            {
                reason = "이전 Output receive 계획이 남아 있습니다.";
                return true;
            }

            return false;
        }

        private static bool CanPreserveInputWaferGenerationForMappingNoLock(
            WaferMaterial wafer,
            out string reason)
        {
            reason = "";
            if (wafer == null)
            {
                reason = "이전 Input wafer가 없습니다.";
                return false;
            }
            if (State == null || State.Wafers == null || State.Dies == null)
            {
                reason = "Material 상태 목록이 없습니다.";
                return false;
            }

            MaterialLocationKind waferLocation = wafer.CurrentLocation != null
                ? wafer.CurrentLocation.Kind
                : MaterialLocationKind.Unknown;
            if (WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty ||
                waferLocation != MaterialLocationKind.Unknown)
            {
                reason = "이전 Input wafer가 Empty/Unknown 보존 세대가 아닙니다.";
                return false;
            }

            string instanceId = (wafer.WaferInstanceId ?? "").Trim();
            string displayId = (wafer.WaferId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(displayId))
            {
                reason = "이전 Input wafer의 물리 identity가 없습니다.";
                return false;
            }
            if (State.Wafers.Count(candidate =>
                    candidate != null &&
                    string.Equals(
                        (candidate.WaferInstanceId ?? "").Trim(),
                        instanceId,
                        StringComparison.OrdinalIgnoreCase)) != 1)
            {
                reason = "이전 Input wafer instance가 State에서 유일하지 않습니다.";
                return false;
            }

            if (State.Cassettes != null)
            {
                foreach (CassetteMaterial cassette in State.Cassettes)
                {
                    if (cassette == null || cassette.Slots == null)
                        continue;

                    foreach (CassetteSlotMaterial slot in cassette.Slots)
                    {
                        if (slot == null)
                            continue;

                        if (!string.IsNullOrWhiteSpace(slot.WaferInstanceId) &&
                            string.Equals(
                                (slot.WaferInstanceId ?? "").Trim(),
                                instanceId,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            reason = "이전 Input wafer가 아직 Cassette slot에 연결되어 있습니다.";
                            return false;
                        }
                        if (string.IsNullOrWhiteSpace(slot.WaferInstanceId) &&
                            !string.IsNullOrWhiteSpace(slot.WaferId) &&
                            string.Equals(
                                (slot.WaferId ?? "").Trim(),
                                displayId,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            reason = "이전 Input wafer를 가리킬 수 있는 legacy slot pointer가 남아 있습니다.";
                            return false;
                        }
                    }
                }
            }

            bool hasDurableOutputReference = false;
            foreach (DieMaterial die in State.Dies)
            {
                if (die == null)
                    continue;

                bool sameInput = !string.IsNullOrWhiteSpace(die.InputWaferInstanceId) &&
                                 string.Equals(
                                     (die.InputWaferInstanceId ?? "").Trim(),
                                     instanceId,
                                     StringComparison.OrdinalIgnoreCase);
                bool legacyInput = string.IsNullOrWhiteSpace(die.InputWaferInstanceId) &&
                                   !string.IsNullOrWhiteSpace(die.WaferID_Input) &&
                                   string.Equals(
                                       (die.WaferID_Input ?? "").Trim(),
                                       displayId,
                                       StringComparison.OrdinalIgnoreCase);
                bool sameOutput = !string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) &&
                                  string.Equals(
                                      (die.OutputWaferInstanceId ?? "").Trim(),
                                      instanceId,
                                      StringComparison.OrdinalIgnoreCase);
                bool legacyOutput = string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) &&
                                    !string.IsNullOrWhiteSpace(die.WaferID_Output) &&
                                    string.Equals(
                                        (die.WaferID_Output ?? "").Trim(),
                                        displayId,
                                        StringComparison.OrdinalIgnoreCase);
                if (legacyInput)
                {
                    reason = "이전 Input wafer를 가리키는 legacy Die pointer가 남아 있습니다. die=" +
                             (die.DieId ?? "");
                    return false;
                }
                if (sameOutput || legacyOutput)
                {
                    reason = "보존 후보 Input wafer가 Die의 Output parent로도 참조됩니다. die=" +
                             (die.DieId ?? "");
                    return false;
                }
                if (!sameInput)
                    continue;

                if (!string.IsNullOrWhiteSpace(die.WaferID_Input) &&
                    !string.Equals(
                        (die.WaferID_Input ?? "").Trim(),
                        displayId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    reason = "이전 Input Die의 instance/display parent가 일치하지 않습니다. die=" +
                             (die.DieId ?? "");
                    return false;
                }

                MaterialLocationKind dieLocation = die.CurrentLocation != null
                    ? die.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                bool allowedOutputHistoryLocation =
                    dieLocation == MaterialLocationKind.Unknown ||
                    dieLocation == MaterialLocationKind.OutputStageGood ||
                    dieLocation == MaterialLocationKind.OutputStageNg ||
                    dieLocation == MaterialLocationKind.OutputFeeder ||
                    dieLocation == MaterialLocationKind.OutputCassette;
                bool activeInputOrPicker = !allowedOutputHistoryLocation ||
                    die.ReservedPickerLocation == MaterialLocationKind.PickerFront ||
                    die.ReservedPickerLocation == MaterialLocationKind.PickerRear ||
                    die.ReservedPickerNo > 0;
                if (activeInputOrPicker)
                {
                    reason = "이전 Input Die가 활성 Input/Picker 위치 또는 예약에 있습니다. die=" +
                             (die.DieId ?? "") + ", location=" + dieLocation;
                    return false;
                }

                string outputInstanceId = (die.OutputWaferInstanceId ?? "").Trim();
                if (string.IsNullOrWhiteSpace(outputInstanceId))
                {
                    if (!string.IsNullOrWhiteSpace(die.WaferID_Output))
                    {
                        reason = "이전 Input Die에 legacy Output parent pointer가 남아 있습니다. die=" +
                                 (die.DieId ?? "");
                        return false;
                    }
                    if (dieLocation != MaterialLocationKind.Unknown)
                    {
                        reason = "Output parent가 없는 이전 Input Die가 활성 위치에 있습니다. die=" +
                                 (die.DieId ?? "") + ", location=" + dieLocation;
                        return false;
                    }

                    // Input clear가 완료된 세대의 passive review/map 항목이다. 새 세대는
                    // WaferInstanceId 기반으로 분리되므로 이전 세대와 함께 보존해도 충돌하지 않는다.
                    continue;
                }

                List<WaferMaterial> outputParents = State.Wafers.Where(candidate =>
                        candidate != null &&
                        string.Equals(
                            (candidate.WaferInstanceId ?? "").Trim(),
                            outputInstanceId,
                            StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToList();
                if (outputParents.Count != 1 || ReferenceEquals(outputParents[0], wafer))
                {
                    reason = "이전 Input Die의 Output parent를 하나로 확인할 수 없습니다. die=" +
                             (die.DieId ?? "") +
                             ", outputInstance=" + outputInstanceId;
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(die.WaferID_Output) &&
                    !string.Equals(
                        (die.WaferID_Output ?? "").Trim(),
                        outputParents[0].WaferId ?? "",
                        StringComparison.OrdinalIgnoreCase))
                {
                    reason = "이전 Input Die의 Output instance/display parent가 일치하지 않습니다. die=" +
                             (die.DieId ?? "");
                    return false;
                }

                hasDurableOutputReference = true;
            }

            foreach (WaferMaterial outputWafer in State.Wafers)
            {
                if (outputWafer == null || ReferenceEquals(outputWafer, wafer))
                    continue;

                bool sameSource = !string.IsNullOrWhiteSpace(outputWafer.OutputReceiveSourceWaferInstanceId) &&
                                  string.Equals(
                                      (outputWafer.OutputReceiveSourceWaferInstanceId ?? "").Trim(),
                                      instanceId,
                                      StringComparison.OrdinalIgnoreCase);
                bool legacySource = string.IsNullOrWhiteSpace(outputWafer.OutputReceiveSourceWaferInstanceId) &&
                                    !string.IsNullOrWhiteSpace(outputWafer.OutputReceiveSourceWaferId) &&
                                    string.Equals(
                                        (outputWafer.OutputReceiveSourceWaferId ?? "").Trim(),
                                        displayId,
                                        StringComparison.OrdinalIgnoreCase);
                if (legacySource)
                {
                    reason = "이전 Input wafer를 가리키는 legacy Output receive source가 남아 있습니다.";
                    return false;
                }
                if (!sameSource)
                    continue;

                if (!string.IsNullOrWhiteSpace(outputWafer.OutputReceiveSourceWaferId) &&
                    !string.Equals(
                        (outputWafer.OutputReceiveSourceWaferId ?? "").Trim(),
                        displayId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    reason = "Output receive source의 instance/display parent가 일치하지 않습니다.";
                    return false;
                }
                hasDurableOutputReference = true;
            }

            if (wafer.OutputReceiveTotalCount > 0 ||
                !string.IsNullOrWhiteSpace(wafer.OutputReceiveSourceWaferInstanceId) ||
                !string.IsNullOrWhiteSpace(wafer.OutputReceiveSourceWaferId) ||
                (wafer.OutputReceiveSlots != null && wafer.OutputReceiveSlots.Count > 0))
            {
                reason = "이전 Input wafer 자체에 Output receive 계획이 남아 있습니다.";
                return false;
            }

            if (!hasDurableOutputReference)
            {
                reason = "새 세대로 분리 보존할 Output 참조가 없습니다.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 새 physical wafer 투입(비보존 매핑) 시 이전 Align/T Align/Mapping/Review 승인과
        /// Pick 진행 정보를 초기화하는 중앙 helper입니다. Slot 고정 WaferId 재사용 상속을 차단합니다.
        /// 동일 wafer의 Stop→Start, snapshot 복구, 단순 재스캔(보존 매핑)에서는 호출하지 않습니다.
        /// </summary>
        private static void ResetInputStageWaferProcessingStateNoLock(WaferMaterial wafer, string cause)
        {
            if (wafer == null)
                return;

            bool hadState = wafer.HasInputStageAlignResult ||
                            wafer.HasInputStageThetaAlignResult ||
                            wafer.HasInputStageDieMappingResult ||
                            wafer.HasInputStageRunReviewApproval ||
                            (wafer.DieIds != null && wafer.DieIds.Count > 0);
            string previousInstanceId = EnsureWaferInstanceIdNoLock(wafer);

            // 이전 wafer의 Die 진행 정보(Result/Picked/예약)를 함께 제거한다.
            string waferId = wafer.WaferId ?? string.Empty;
            int removedForReset = State.Dies.RemoveAll(d =>
                IsInputOnlyDieForWaferInstanceNoLock(d, waferId, previousInstanceId));
            if (removedForReset > 0)
                InvalidateDieByIdIndexNoLock();
            // 세대 리셋은 키 필드(Generation 등)로도 무효화되지만, 명시적으로도 비운다 (belt & braces).
            InvalidateInputPickContextCacheNoLock();

            ClearInputStageWaferProcessingFieldsNoLock(wafer);
            wafer.DieIds = new List<string>();
            wafer.InputStageProcessingGeneration = wafer.InputStageProcessingGeneration + 1;
            wafer.WaferInstanceId = CreateWaferInstanceId();
            InputStageHybridResultSession.Clear();

            if (hadState)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "New physical wafer processing state reset. wafer=" + (wafer.WaferId ?? "") +
                    ", previousInstance=" + previousInstanceId +
                    ", currentInstance=" + wafer.WaferInstanceId +
                    ", generation=" + wafer.InputStageProcessingGeneration +
                    ", cause=" + (cause ?? "") + " - Ok");
            }
        }

        /// <summary>
        /// Input Stage의 Align/Mapping/Review 결과 필드만 초기화한다.
        /// Die 삭제 범위는 호출 목적마다 다르므로 이 helper에서는 Die 목록을 직접 지우지 않는다.
        /// </summary>
        private static void ClearInputStageWaferProcessingFieldsNoLock(WaferMaterial wafer)
        {
            if (wafer == null)
                return;

            wafer.InputResultFileSessionStartedAt = null;
            wafer.HasInputStageAlignResult = false;
            wafer.InputStageAlignResultMode = string.Empty;
            wafer.InputStageAlignResultRunId = string.Empty;
            wafer.InputStageAlignOriginX = 0.0;
            wafer.InputStageAlignOriginY = 0.0;
            wafer.InputStageAlignPitchX = 0.0;
            wafer.InputStageAlignPitchY = 0.0;
            wafer.InputStageDieSizeX = 0.0;
            wafer.InputStageDieSizeY = 0.0;
            wafer.InputStageOuterDiameterMm = 0.0;
            wafer.InputStageAlignOffsetX = 0.0;
            wafer.InputStageAlignOffsetY = 0.0;
            wafer.HasInputStageThetaAlignResult = false;
            wafer.InputStageAlignReferenceT = 0.0;
            wafer.InputStageAlignCorrectedT = 0.0;
            wafer.InputStageAlignOffsetT = 0.0;
            wafer.HasInputStageDieMappingResult = false;
            wafer.InputStageDieMappingResultMode = string.Empty;
            wafer.InputStageDieMappingAlignRunId = string.Empty;
            wafer.InputStageDieMappingOffsetX = 0.0;
            wafer.InputStageDieMappingOffsetY = 0.0;
            wafer.HasInputStageDieMappingOrigin = false;
            wafer.InputStageDieMappingOriginX = 0.0;
            wafer.InputStageDieMappingOriginY = 0.0;
            wafer.HasInputStageDieMappingThetaSnapshot = false;
            wafer.InputStageDieMappingCorrectedT = 0.0;
            wafer.InputStageDieMappingInvalidatedByAlignChange = false;
            wafer.InputMapApprovalHashAtMapping = string.Empty;
            wafer.DieMapFrameObjId = string.Empty;
            wafer.HasInputStageRunReviewApproval = false;
            wafer.InputStageRunReviewStartDieIndex = 0;
            wafer.InputStageRunReviewStartDieUid = string.Empty;
            wafer.InputStageRunReviewOrderedDieIds = new List<string>();
            wafer.InputStageRunReviewMappingRevision = string.Empty;
        }

        private static bool IsWaferAtCassetteSlot(WaferMaterial wafer, CassetteMaterialRole role, int slotNumber)
        {
            if (wafer == null || wafer.CurrentLocation == null)
                return false;

            MaterialLocationKind expectedKind = IsOutputCassetteRole(role)
                ? MaterialLocationKind.OutputCassette
                : MaterialLocationKind.InputCassette;
            return wafer.CurrentLocation.Kind == expectedKind &&
                   wafer.CurrentLocation.CassetteRole == role &&
                   wafer.CurrentLocation.SlotNumber == slotNumber;
        }

        private static bool IsFinishedOutputBinWafer(CassetteMaterialRole role, WaferMaterial wafer)
        {
            return IsOutputCassetteRole(role) &&
                   wafer != null &&
                   WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Finish;
        }

        private static bool IsOutputCassetteRole(CassetteMaterialRole role)
        {
            return role == CassetteMaterialRole.Good1 ||
                   role == CassetteMaterialRole.Good2 ||
                   role == CassetteMaterialRole.Ng1;
        }

        private static void ValidateOutputMappingDetachCandidatesNoLock(
            CassetteMaterialRole role,
            bool ignoreProcessTestStageMaterial = false)
        {
            if (!IsOutputCassetteRole(role) || State == null || State.Wafers == null)
                return;

            List<WaferMaterial> candidates = State.Wafers
                .Where(wafer =>
                {
                    if (wafer == null)
                        return false;

                    if (ignoreProcessTestStageMaterial &&
                        wafer.CurrentLocation != null &&
                        (wafer.CurrentLocation.Kind == MaterialLocationKind.InputStage ||
                         wafer.CurrentLocation.Kind == MaterialLocationKind.OutputStageGood ||
                         wafer.CurrentLocation.Kind == MaterialLocationKind.OutputStageNg))
                    {
                        return false;
                    }

                    WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
                    bool atRoleCassette = wafer.CurrentLocation != null &&
                                          wafer.CurrentLocation.Kind == MaterialLocationKind.OutputCassette &&
                                          wafer.CurrentLocation.CassetteRole == role;
                    bool belongsToRole = wafer.SourceCassetteRole == role ||
                                         (!string.IsNullOrWhiteSpace(wafer.OutputCassetteId) &&
                                          wafer.OutputCassetteRole == role);
                    return atRoleCassette ||
                           IsDetachedWorkingOutputMaterial(wafer, role) ||
                           ((state == WaferMaterialState.Empty || state == WaferMaterialState.Finish) &&
                            belongsToRole);
                })
                .Distinct()
                .ToList();
            if (candidates.Count == 0 || State.Dies == null)
                return;

            List<DieMaterial> relatedDies;
            string collectReason;
            if (!TryCollectOutputParentDiesNoLock(candidates, out relatedDies, out collectReason))
            {
                throw new InvalidOperationException(
                    "Output cassette Mapping 교체 대상의 Output parent를 안전하게 확인할 수 없습니다. cassette=" +
                    role + ", detail=" + collectReason);
            }
            foreach (DieMaterial die in relatedDies)
            {
                MaterialLocationKind locationKind = die != null && die.CurrentLocation != null
                    ? die.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                bool hasActiveReservation = die != null &&
                    (die.ReservedPickerLocation == MaterialLocationKind.PickerFront ||
                     die.ReservedPickerLocation == MaterialLocationKind.PickerRear ||
                     die.ReservedPickerNo > 0);
                if ((locationKind != MaterialLocationKind.Unknown &&
                     locationKind != MaterialLocationKind.OutputCassette) ||
                    hasActiveReservation)
                {
                    throw new InvalidOperationException(
                        "Output cassette Mapping 교체 대상 Die가 다른 활성 위치/예약에 있습니다. cassette=" +
                        role + ", die=" + (die != null ? die.DieId : "") +
                        ", location=" + locationKind +
                        ", reserved=" + (die != null ? die.ReservedPickerLocation + "/" + die.ReservedPickerNo : ""));
                }
            }
            List<DieMaterial> preserveDies;
            List<DieMaterial> removeDies;
            string reason;
            if (!TryClassifyOutputDiesForDetachNoLock(
                relatedDies,
                out preserveDies,
                out removeDies,
                out reason))
            {
                throw new InvalidOperationException(
                    "Output cassette Mapping 교체 대상 Material을 안전하게 분리할 수 없습니다. cassette=" +
                    role + ", detail=" + reason);
            }
        }

        private static void DetachOutputWaferMaterialForReplacementNoLock(WaferMaterial wafer)
        {
            if (wafer == null || State == null || State.Dies == null)
                return;

            List<DieMaterial> relatedDies;
            string reason;
            if (!TryCollectOutputParentDiesNoLock(
                new List<WaferMaterial> { wafer },
                out relatedDies,
                out reason))
            {
                throw new InvalidOperationException(
                    "Output Material replacement parent를 안전하게 확인할 수 없습니다. wafer=" +
                    (wafer.WaferId ?? "") + ", detail=" + reason);
            }
            DetachOrRemoveClearedOutputDiesNoLock(relatedDies);
            ClearOutputStageWaferProcessingFieldsNoLock(wafer);
        }

        private static bool IsDieOutputParentForWaferNoLock(
            DieMaterial die,
            WaferMaterial wafer)
        {
            if (die == null || wafer == null)
                return false;

            string outputInstance = (die.OutputWaferInstanceId ?? "").Trim();
            string waferInstance = (wafer.WaferInstanceId ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(outputInstance))
            {
                return !string.IsNullOrWhiteSpace(waferInstance) &&
                       string.Equals(outputInstance, waferInstance, StringComparison.OrdinalIgnoreCase);
            }

            string outputDisplay = (die.WaferID_Output ?? "").Trim();
            if (string.IsNullOrWhiteSpace(outputDisplay) ||
                !string.Equals(outputDisplay, wafer.WaferId ?? "", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return State.Wafers.Count(candidate =>
                candidate != null &&
                string.Equals(candidate.WaferId ?? "", outputDisplay, StringComparison.OrdinalIgnoreCase)) == 1;
        }

        private static bool IsDetachedWorkingOutputMaterial(
            WaferMaterial wafer,
            CassetteMaterialRole role)
        {
            if (wafer == null ||
                !IsOutputCassetteRole(role) ||
                wafer.SourceCassetteRole != role ||
                WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Working)
            {
                return false;
            }

            return wafer.CurrentLocation == null ||
                   wafer.CurrentLocation.Kind == MaterialLocationKind.Unknown;
        }

        private static void RemoveDetachedWorkingOutputMaterialForMapping(
            CassetteMaterialRole role)
        {
            List<WaferMaterial> staleWafers = State.Wafers
                .Where(w => IsDetachedWorkingOutputMaterial(w, role))
                .ToList();

            foreach (WaferMaterial wafer in staleWafers)
            {
                string waferId = wafer.WaferId ?? string.Empty;
                int sourceSlot = wafer.SourceSlotNumber;
                List<DieMaterial> relatedDies = State.Dies
                    .Where(d => IsDieOutputParentForWaferNoLock(d, wafer))
                    .ToList();
                int removedDieCount = DetachOrRemoveClearedOutputDiesNoLock(relatedDies);

                RemoveWaferFromCassetteSlot(wafer);
                State.Wafers.Remove(wafer);

                Log.Write(
                    "Main",
                    "SYSTEM",
                    "OutputCassetteMappingRecovery",
                    "Detached Working/Unknown material removed after successful physical scan. cassette=" + role +
                    ", wafer=" + waferId +
                    ", sourceSlot=" + (sourceSlot + 1) +
                    ", removedDies=" + removedDieCount +
                    " - Check");
            }
        }

        private static void RemoveFinishedOutputBinWaferForNewCassetteMapping(WaferMaterial wafer)
        {
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
                return;

            List<DieMaterial> relatedDies = State.Dies
                .Where(d => IsDieOutputParentForWaferNoLock(d, wafer))
                .ToList();
            DetachOrRemoveClearedOutputDiesNoLock(relatedDies);
            State.Wafers.Remove(wafer);
        }

        private static string ResolveDefaultTapeFrameSpecName(int inchSelect)
        {
            double targetDiameter = inchSelect == 1 ? 300 : 200;
            var spec = MaterialSpecs.Data != null && MaterialSpecs.Data.Frames != null
                ? MaterialSpecs.Data.Frames.FirstOrDefault(f => Math.Abs(f.OuterDiameterMm - targetDiameter) < 0.001)
                : null;
            return spec != null ? spec.Name : (inchSelect == 1 ? "12inch_50x50" : "8inch_5x5");
        }

        private static void EnsureTapeFrameSpecFromRecipe(RecipeProject project, string specName)
        {
            if (project == null || project.Frame == null || string.IsNullOrWhiteSpace(specName))
                return;

            EnsureTapeFrameSpecFromFrame(project, project.Frame, specName, "");
        }

        private static void EnsureTapeFrameSpecFromFrame(RecipeProject project, TapeFrameSubset frame, string specName, string mapFileName)
        {
            if (project == null || frame == null || string.IsNullOrWhiteSpace(specName))
                return;

            if (MaterialSpecs.Data == null)
                return;

            EnsureDieSpecFromRecipe(project, project.Die != null ? project.Die.DieSpecName : "");
            bool externalMap = RecipeDieMapResolver.IsExternalFrame(frame);
            int dieMapX = externalMap
                ? Math.Max(1, frame.DieMapX)
                : ResolvePitchBasedGridCount(
                    frame.OuterDiameterMm,
                    DieMapGenerator.CalculateCenterStep(frame.DieSizeX, frame.PitchX),
                    frame.DieSizeX,
                    frame.DieMapX);
            int dieMapY = externalMap
                ? Math.Max(1, frame.DieMapY)
                : ResolvePitchBasedGridCount(
                    frame.OuterDiameterMm,
                    DieMapGenerator.CalculateCenterStep(frame.DieSizeY, frame.PitchY),
                    frame.DieSizeY,
                    frame.DieMapY);

            MaterialSpecs.UpsertFrame(
                specName,
                dieMapX,
                dieMapY,
                frame.PitchX,
                frame.PitchY,
                frame.DieSizeX,
                frame.DieSizeY,
                frame.OuterDiameterMm,
                frame.EdgeSkipMode,
                frame.SideEdgeSkip,
                frame.TopBottomEdgeSkip,
                frame.SideEdgeSkipMm,
                frame.TopBottomEdgeSkipMm,
                mapFileName,
                project.Die != null ? project.Die.DieSpecName ?? "" : "");
        }

        private static int ResolvePitchBasedGridCount(
            double outerDiameterMm,
            double centerStepMm,
            double dieSizeMm,
            int fallback)
        {
            if (outerDiameterMm <= 0.0 || centerStepMm <= 0.0 || dieSizeMm <= 0.0)
                return Math.Max(1, fallback);

            return DieMapGenerator.CalculateWaferGridCount(outerDiameterMm, centerStepMm, dieSizeMm);
        }

        private static void EnsureDieSpecFromRecipe(RecipeProject project, string specName)
        {
            if (project == null || project.Die == null)
                return;

            if (MaterialSpecs.Data == null)
                return;

            var die = project.Die;
            MaterialSpecs.UpsertDie(
                string.IsNullOrWhiteSpace(specName) ? die.DieSpecName : specName,
                die.WidthMm,
                die.HeightMm,
                die.ThicknessMm,
                die.ChipLowerSpecLimitWidth,
                die.ChipUpperSpecLimitWidth,
                die.ChipLowerSpecLimitHeight,
                die.ChipUpperSpecLimitHeight,
                die.ChippingDepthMax,
                die.ChippingLengthMax,
                die.ForeignSizeMax);
        }

        private static string ResolveCassetteTapeFrameSpecName(CassetteMaterial cassette)
        {
            if (cassette == null || cassette.Slots == null)
                return ResolveRecipeTapeFrameSpecName(0);

            foreach (var slot in cassette.Slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.WaferId))
                    continue;

                string resolveReason;
                var wafer = ResolveCassetteSlotWaferNoLock(slot, out resolveReason);
                if (wafer != null && !string.IsNullOrWhiteSpace(wafer.TapeFrameSpecName))
                    return wafer.TapeFrameSpecName;
            }

            return ResolveRecipeTapeFrameSpecName(0);
        }

        private static void ApplyWaferCassetteLocation(WaferMaterial wafer, CassetteMaterial cassette, int slotNumber, string cassetteLotId, double slotPosition = double.NaN)
        {
            if (wafer == null || cassette == null) return;

            string resolvedLotId = ResolveOrCreateCassetteLotId(cassetteLotId, cassette, wafer);
            wafer.CassetteLotId = resolvedLotId;
            cassette.CassetteLotId = resolvedLotId;

            wafer.SourceCassetteId = cassette.CassetteId;
            wafer.SourceCassetteRole = cassette.Role;
            wafer.SourceSlotNumber = slotNumber;
            ApplyWaferCassettePosition(wafer, slotPosition);
            // 기존 조건: Kind를 InputCassette로 고정 — Output Role(Good1/Good2/Ng1)에서 호출되면
            //           CurrentLocation.Kind가 InputCassette로 오염되어 위치 판정이 어긋났다.
            // 현재 기준: Role에 따라 Input/Output Cassette Kind를 구분한다(PutWaferInCassette와 동일 규칙).
            wafer.CurrentLocation = MaterialLocation.Cassette(
                IsOutputCassetteRole(cassette.Role) ? MaterialLocationKind.OutputCassette : MaterialLocationKind.InputCassette,
                cassette.Role,
                slotNumber);
            wafer.State = wafer.State == WaferMaterialState.Empty
                ? WaferMaterialState.Ready
                : WaferMaterialStateText.Normalize(wafer.State);
            wafer.UpdatedAt = DateTime.Now;
        }

        public static bool TryGetCassetteSlotPosition(CassetteMaterialRole cassetteRole, int slotNumber, out double slotPosition)
        {
            slotPosition = double.NaN;
            try
            {
                var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
                if (cassette == null || !cassette.IsMapped || cassette.Slots == null)
                    return false;
                if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                    return false;

                var slot = cassette.Slots[slotNumber];
                string slotReason;
                var wafer = slot != null && slot.HasWafer && !string.IsNullOrWhiteSpace(slot.WaferId)
                    ? ResolveCassetteSlotWaferNoLock(slot, out slotReason)
                    : null;
                if (wafer == null || double.IsNaN(wafer.CurrentCassetteSlotPosition))
                    return false;

                slotPosition = wafer.CurrentCassetteSlotPosition;
                return true;
            }
            catch
            {
                slotPosition = double.NaN;
                return false;
            }
            finally
            {
            }
        }

        private static void ApplyWaferCassettePosition(WaferMaterial wafer, double slotPosition)
        {
            if (wafer == null || double.IsNaN(slotPosition))
                return;

            wafer.SourceCassetteSlotPosition = slotPosition;
            wafer.CurrentCassetteSlotPosition = slotPosition;
        }

        // 공정 테스트 Data용 슬롯별 카세트 포지션 계산.
        // 티칭 미완 등으로 계산이 실패하면 포지션 없이(null) 생성하도록 하고 사유를 로그에 남긴다.
        private static double[] BuildProcessTestSlotPositions(int slotCount, Func<int, double> calculate, string label)
        {
            if (slotCount <= 0 || calculate == null)
                return null;

            try
            {
                var positions = new double[slotCount];
                for (int i = 0; i < slotCount; i++)
                    positions[i] = calculate(i);
                return positions;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "공정 테스트 슬롯 포지션 계산에 실패해 포지션 없이 생성합니다. cassette=" + label +
                    ", error=" + ex.Message + " - Check");
                return null;
            }
            finally
            {
            }
        }

        private static double ResolveSlotPosition(IReadOnlyList<double> slotPositions, int slotNumber)
        {
            if (slotPositions == null || slotNumber < 0 || slotNumber >= slotPositions.Count)
                return double.NaN;

            return slotPositions[slotNumber];
        }

        private static CassetteMaterial EnsureCassette(CassetteMaterialRole role, int slotCount)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == role);
            if (cassette != null)
            {
                cassette.CassetteId = string.IsNullOrEmpty(cassette.CassetteId) ? role.ToString() : cassette.CassetteId;
                cassette.Level = role == CassetteMaterialRole.Input2 || role == CassetteMaterialRole.Good2 ? 2 : 1;
                cassette.SlotCount = slotCount;
                cassette.EnsureSlots();
                return cassette;
            }

            cassette = new CassetteMaterial
            {
                CassetteId = role.ToString(),
                Role = role,
                Level = role == CassetteMaterialRole.Input2 || role == CassetteMaterialRole.Good2 ? 2 : 1,
                SlotCount = slotCount
            };
            cassette.EnsureSlots();
            State.Cassettes.Add(cassette);
            return cassette;
        }

        /// <summary>
        /// 바코드가 없는 운용에서 카세트 맵핑 시 부여하는 임의 WaferId를 만든다.
        /// 형식: {ROLE}-S{슬롯2자리}-{MMddHHmmss} (예: INPUT1-S01-0805143022)
        /// 기존에는 role+slot만 사용해 재맵핑마다 동일 ID가 생성되어 서로 다른 웨이퍼를
        /// 구분할 수 없었다. 생성 시각(월일시분초)을 붙여 맵핑마다 다른 ID가 되게 한다.
        /// 연도는 제외한다(길이 절약, 사용자 확정). 구분자는 '-'만 사용해 파일명 안전을 유지한다.
        /// Input/Output(GOOD/NG) 카세트 공용이다.
        /// </summary>
        internal static string BuildGeneratedWaferId(CassetteMaterialRole role, int slotNumber)
        {
            string prefix = role.ToString().ToUpperInvariant() + "-S" + (slotNumber + 1).ToString("00");
            string stamp = DateTime.Now.ToString("MMddHHmmss", CultureInfo.InvariantCulture);
            string candidate = prefix + "-" + stamp;

            // 같은 슬롯을 같은 초에 다시 맵핑하는 극단 케이스만 순번으로 회피한다.
            // (동일 맵핑 내 다른 슬롯은 슬롯 번호가 이미 ID에 있어 충돌하지 않는다.)
            if (!ExistsWaferIdNoLock(candidate))
                return candidate;

            for (int seq = 2; seq <= 99; seq++)
            {
                string retry = candidate + "-" + seq.ToString(CultureInfo.InvariantCulture);
                if (!ExistsWaferIdNoLock(retry))
                    return retry;
            }

            return candidate;
        }

        private static bool ExistsWaferIdNoLock(string waferId)
        {
            try
            {
                if (State == null || State.Wafers == null)
                    return false;

                return State.Wafers.Any(w =>
                    w != null && string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }
    }
}
