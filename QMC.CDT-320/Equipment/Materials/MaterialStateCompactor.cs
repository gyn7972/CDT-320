using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT320.Materials
{
    internal sealed class MaterialCompactionResult
    {
        public int BeforeWaferCount { get; set; }
        public int BeforeDieCount { get; set; }
        public int RemovedWaferCount { get; set; }
        public int RemovedDieCount { get; set; }
        public int WarningCount { get; private set; }
        public string FirstWarning { get; private set; } = "";

        public bool Changed
        {
            get { return RemovedWaferCount > 0 || RemovedDieCount > 0; }
        }

        public int AfterWaferCount
        {
            get { return BeforeWaferCount - RemovedWaferCount; }
        }

        public int AfterDieCount
        {
            get { return BeforeDieCount - RemovedDieCount; }
        }

        internal void AddWarning(string warning)
        {
            WarningCount++;
            if (string.IsNullOrWhiteSpace(FirstWarning))
                FirstWarning = warning ?? "";
        }
    }

    /// <summary>
    /// 현재 장비에 존재하거나 재시작 복구에 필요한 Material graph만 남긴다.
    /// 호출자는 Material 상태 락을 보유해야 하며 Pick/Place/Mapping 생성 중간에는 호출하지 않는다.
    /// </summary>
    internal static class MaterialStateCompactor
    {
        private const int MinimumPickerNo = 1;
        private const int MaximumPickerNo = 4;

        public static MaterialCompactionResult Compact(
            MaterialSnapshot snapshot,
            IEnumerable<string> externalDieRootIds = null)
        {
            var result = new MaterialCompactionResult();
            if (snapshot == null)
            {
                result.AddWarning("Material snapshot이 없습니다.");
                return result;
            }

            if (snapshot.Cassettes == null)
                snapshot.Cassettes = new List<CassetteMaterial>();
            if (snapshot.Wafers == null)
                snapshot.Wafers = new List<WaferMaterial>();
            if (snapshot.Dies == null)
                snapshot.Dies = new List<DieMaterial>();

            result.BeforeWaferCount = snapshot.Wafers.Count;
            result.BeforeDieCount = snapshot.Dies.Count;

            var context = new MaterialGraphContext(snapshot, result);
            context.SeedRoots(externalDieRootIds);
            context.ResolveReachableGraph();
            context.RemoveUnreachableMaterials();
            PruneDanglingInputStageRunReviewOrder(snapshot, result);
            return result;
        }

        /// <summary>
        /// [정합성 복구 2026-08-09] Wafer 의 Input Stage Review 승인 PickUp 순서에서, 더 이상 존재하지 않거나
        /// 그 Wafer 의 Die 가 아닌 UID 를 제거한다. 반드시 죽은 Die 제거(RemoveUnreachableMaterials) 뒤에 실행한다.
        ///
        /// 배경: Review 승인 순서(InputStageRunReviewOrderedDieIds)와 시작 Die pointer 는 승인 시점의 Die UID 를
        ///   그대로 들고 있는데, 이후 Clear/재매핑으로 그 Die 가 State 에서 사라지면 목록만 옛 UID 를 붙들게 된다.
        ///   저장 직전 TryValidateForSave 의 "Input Stage review 순서가 해당 Input Wafer 의 Die 를 가리키지 않습니다"
        ///   검사에 걸려 이후 모든 저장이 영구 거부된다.
        ///   실측: 2026-08-08 5시간, 2026-08-09 5.7시간(연속 실패 3,163회) 동안 자재 상태가 디스크에 반영되지 않았다.
        ///   컴팩션은 저장 경로에 없고 Clear/재매핑/이동/로드 경로에만 있어, 한번 어긋나면 재기동 전까지 낫지 않았다.
        ///
        /// 안전: 승인 자체를 임의로 해제하지 않는다. 살아 있는 Die 로 이루어진 순서가 남으면 그대로 유지하고,
        ///   순서가 모두 사라졌을 때만 승인을 해제해 작업자 재확인을 요구한다(fail-closed).
        /// </summary>
        private static void PruneDanglingInputStageRunReviewOrder(
            MaterialSnapshot snapshot,
            MaterialCompactionResult result)
        {
            try
            {
                if (snapshot.Wafers.Count == 0)
                    return;

                var waferByInstance = new Dictionary<string, WaferMaterial>(StringComparer.OrdinalIgnoreCase);
                var wafersByDisplayId = new Dictionary<string, List<WaferMaterial>>(StringComparer.OrdinalIgnoreCase);
                foreach (WaferMaterial wafer in snapshot.Wafers)
                {
                    if (wafer == null)
                        continue;

                    string instanceId = NormalizeId(wafer.WaferInstanceId);
                    if (!string.IsNullOrWhiteSpace(instanceId) && !waferByInstance.ContainsKey(instanceId))
                        waferByInstance.Add(instanceId, wafer);

                    string displayId = NormalizeId(wafer.WaferId);
                    if (string.IsNullOrWhiteSpace(displayId))
                        continue;

                    List<WaferMaterial> displayCandidates;
                    if (!wafersByDisplayId.TryGetValue(displayId, out displayCandidates))
                    {
                        displayCandidates = new List<WaferMaterial>();
                        wafersByDisplayId.Add(displayId, displayCandidates);
                    }
                    displayCandidates.Add(wafer);
                }

                var dieById = new Dictionary<string, DieMaterial>(StringComparer.OrdinalIgnoreCase);
                foreach (DieMaterial die in snapshot.Dies)
                {
                    if (die == null)
                        continue;

                    string dieId = NormalizeId(die.DieId);
                    if (!string.IsNullOrWhiteSpace(dieId) && !dieById.ContainsKey(dieId))
                        dieById.Add(dieId, die);
                }

                foreach (WaferMaterial wafer in snapshot.Wafers)
                {
                    if (wafer == null)
                        continue;

                    int removedFromOrder = 0;
                    if (wafer.InputStageRunReviewOrderedDieIds != null &&
                        wafer.InputStageRunReviewOrderedDieIds.Count > 0)
                    {
                        var keptIds = new List<string>(wafer.InputStageRunReviewOrderedDieIds.Count);
                        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (string rawDieId in wafer.InputStageRunReviewOrderedDieIds)
                        {
                            string dieId = NormalizeId(rawDieId);
                            DieMaterial reviewDie;
                            if (!string.IsNullOrWhiteSpace(dieId) &&
                                seenIds.Add(dieId) &&
                                dieById.TryGetValue(dieId, out reviewDie) &&
                                DoesDieReferenceWafer(
                                    reviewDie,
                                    wafer,
                                    true,
                                    false,
                                    waferByInstance,
                                    wafersByDisplayId))
                            {
                                keptIds.Add(rawDieId);
                                continue;
                            }

                            removedFromOrder++;
                        }

                        if (removedFromOrder > 0)
                            wafer.InputStageRunReviewOrderedDieIds = keptIds;
                    }

                    // 시작 Die pointer 도 같은 규칙으로 검사한다. 끊어졌으면 지정 없음(index=0)으로 되돌린다.
                    bool startPointerCleared = false;
                    string startDieId = NormalizeId(wafer.InputStageRunReviewStartDieUid);
                    if (!string.IsNullOrWhiteSpace(startDieId))
                    {
                        DieMaterial startDie;
                        if (!dieById.TryGetValue(startDieId, out startDie) ||
                            !DoesDieReferenceWafer(
                                startDie,
                                wafer,
                                true,
                                false,
                                waferByInstance,
                                wafersByDisplayId))
                        {
                            wafer.InputStageRunReviewStartDieUid = "";
                            wafer.InputStageRunReviewStartDieIndex = 0;
                            startPointerCleared = true;
                        }
                    }

                    if (removedFromOrder == 0 && !startPointerCleared)
                        continue;

                    // 시작 Die 가 유효해도 승인 순서 첫 항목과 어긋나면 검증에서 막히므로 지정을 해제한다.
                    if (!startPointerCleared &&
                        !string.IsNullOrWhiteSpace(wafer.InputStageRunReviewStartDieUid) &&
                        (wafer.InputStageRunReviewOrderedDieIds == null ||
                         wafer.InputStageRunReviewOrderedDieIds.Count == 0 ||
                         !string.Equals(
                             NormalizeId(wafer.InputStageRunReviewOrderedDieIds[0]),
                             NormalizeId(wafer.InputStageRunReviewStartDieUid),
                             StringComparison.OrdinalIgnoreCase)))
                    {
                        wafer.InputStageRunReviewStartDieUid = "";
                        wafer.InputStageRunReviewStartDieIndex = 0;
                    }

                    // 승인된 PickUp 대상이 하나도 남지 않으면 승인을 해제해 작업자 재확인을 요구한다.
                    bool approvalCleared = false;
                    if (wafer.HasInputStageRunReviewApproval &&
                        (wafer.InputStageRunReviewOrderedDieIds == null ||
                         wafer.InputStageRunReviewOrderedDieIds.Count == 0))
                    {
                        wafer.HasInputStageRunReviewApproval = false;
                        wafer.InputStageRunReviewMappingRevision = "";
                        wafer.InputStageRunReviewStartDieUid = "";
                        wafer.InputStageRunReviewStartDieIndex = 0;
                        approvalCleared = true;
                    }

                    // UpdatedAt 갱신으로 Wafer 상태 키가 바뀌므로 InputPickContext 캐시는 자동 무효화된다.
                    // Die 집합은 건드리지 않으므로 DieId 인덱스는 그대로 유효하다.
                    wafer.UpdatedAt = DateTime.Now;
                    result.AddWarning(
                        "Input Stage Review 승인 데이터에서 사라진 Die 참조를 정리했습니다. wafer=" +
                        (wafer.WaferId ?? "") +
                        ", removedOrder=" + removedFromOrder +
                        ", startPointerCleared=" + startPointerCleared +
                        ", approvalCleared=" + approvalCleared);
                }
            }
            catch (Exception ex)
            {
                result.AddWarning("Input Stage Review 승인 데이터 정리 실패: " + ex.Message);
            }
        }

        public static bool TryValidateForSave(MaterialSnapshot snapshot, out string reason)
        {
            reason = "";
            if (snapshot == null)
            {
                reason = "snapshot이 없습니다.";
                return false;
            }
            if (snapshot.Cassettes == null || snapshot.Wafers == null || snapshot.Dies == null)
            {
                reason = "Cassettes/Wafers/Dies 목록이 초기화되지 않았습니다.";
                return false;
            }
            if (snapshot.Version < MaterialSnapshot.MinimumSupportedVersion ||
                snapshot.Version > MaterialSnapshot.CurrentVersion)
            {
                reason = "지원하지 않는 Material snapshot 버전입니다. version=" + snapshot.Version;
                return false;
            }
            if (!MaterialSnapshotRevisionPolicy.IsTrustedLoadedRevision(snapshot.SnapshotRevision))
            {
                reason = "SnapshotRevision이 신뢰 가능한 범위를 벗어났습니다. revision=" +
                         snapshot.SnapshotRevision;
                return false;
            }
            if (!TryValidateRawMaterialValues(snapshot, out reason))
                return false;

            var waferByInstance = new Dictionary<string, WaferMaterial>(StringComparer.OrdinalIgnoreCase);
            var wafersByDisplayId = new Dictionary<string, List<WaferMaterial>>(StringComparer.OrdinalIgnoreCase);
            foreach (WaferMaterial wafer in snapshot.Wafers)
            {
                if (wafer == null)
                {
                    reason = "Wafers 목록에 null 항목이 있습니다.";
                    return false;
                }

                string instanceId = NormalizeId(wafer.WaferInstanceId);
                if (string.IsNullOrWhiteSpace(instanceId))
                {
                    reason = "WaferInstanceId가 비어 있습니다. wafer=" + (wafer.WaferId ?? "");
                    return false;
                }
                if (waferByInstance.ContainsKey(instanceId))
                {
                    reason = "WaferInstanceId가 중복되었습니다. instance=" + instanceId;
                    return false;
                }
                waferByInstance.Add(instanceId, wafer);

                string displayId = NormalizeId(wafer.WaferId);
                if (!string.IsNullOrWhiteSpace(displayId))
                {
                    List<WaferMaterial> displayCandidates;
                    if (!wafersByDisplayId.TryGetValue(displayId, out displayCandidates))
                    {
                        displayCandidates = new List<WaferMaterial>();
                        wafersByDisplayId.Add(displayId, displayCandidates);
                    }
                    displayCandidates.Add(wafer);
                }
            }

            var dieById = new Dictionary<string, DieMaterial>(StringComparer.OrdinalIgnoreCase);
            foreach (DieMaterial die in snapshot.Dies)
            {
                if (die == null)
                {
                    reason = "Dies 목록에 null 항목이 있습니다.";
                    return false;
                }

                string dieId = NormalizeId(die.DieId);
                if (string.IsNullOrWhiteSpace(dieId))
                {
                    reason = "DieId가 비어 있습니다.";
                    return false;
                }
                if (dieById.ContainsKey(dieId))
                {
                    reason = "DieId가 중복되었습니다. die=" + dieId;
                    return false;
                }
                dieById.Add(dieId, die);
            }

            if (!ValidateCassettePointers(snapshot, waferByInstance, out reason))
                return false;
            if (!ValidateMaterialParentPointers(snapshot, waferByInstance, wafersByDisplayId, out reason))
                return false;
            if (!ValidateWaferDiePointers(snapshot, dieById, waferByInstance, wafersByDisplayId, out reason))
                return false;
            if (!ValidateActiveLocations(snapshot, out reason))
                return false;

            return true;
        }

        private static bool ValidateCassettePointers(
            MaterialSnapshot snapshot,
            IDictionary<string, WaferMaterial> waferByInstance,
            out string reason)
        {
            reason = "";
            var cassetteByRole = new Dictionary<CassetteMaterialRole, CassetteMaterial>();
            foreach (CassetteMaterial cassette in snapshot.Cassettes)
            {
                if (cassette == null || cassette.Slots == null)
                {
                    reason = "Cassette 또는 Slot 목록이 초기화되지 않았습니다.";
                    return false;
                }

                if (cassetteByRole.ContainsKey(cassette.Role))
                {
                    reason = "같은 역할의 Cassette Material이 둘 이상 있습니다. cassette=" + cassette.Role;
                    return false;
                }
                cassetteByRole.Add(cassette.Role, cassette);
                if (cassette.SlotCount < 0 || cassette.Slots.Count != cassette.SlotCount)
                {
                    reason = "Cassette SlotCount와 Slot 목록 크기가 일치하지 않습니다. cassette=" +
                             cassette.Role + ", slotCount=" + cassette.SlotCount +
                             ", items=" + cassette.Slots.Count;
                    return false;
                }

                for (int slotIndex = 0; slotIndex < cassette.Slots.Count; slotIndex++)
                {
                    CassetteSlotMaterial slot = cassette.Slots[slotIndex];
                    if (slot == null)
                    {
                        reason = "Cassette Slot에 null 항목이 있습니다. cassette=" + cassette.Role;
                        return false;
                    }
                    if (slot.SlotNumber != slotIndex)
                    {
                        reason = "Cassette Slot 번호가 목록 위치와 일치하지 않습니다. cassette=" +
                                 cassette.Role + ", index=" + slotIndex +
                                 ", slotNumber=" + slot.SlotNumber;
                        return false;
                    }

                    bool hasWaferId = !string.IsNullOrWhiteSpace(slot.WaferId);
                    bool hasInstanceId = !string.IsNullOrWhiteSpace(slot.WaferInstanceId);
                    if (!hasWaferId && !hasInstanceId)
                    {
                        if (slot.HasWafer)
                        {
                            reason = "점유 Slot에 Material pointer가 없습니다. cassette=" + cassette.Role +
                                     ", slot=" + (slot.SlotNumber + 1);
                            return false;
                        }
                        continue;
                    }

                    if (!hasWaferId || !hasInstanceId)
                    {
                        reason = "Slot의 Material pointer가 불완전합니다. cassette=" + cassette.Role +
                                 ", slot=" + (slot.SlotNumber + 1);
                        return false;
                    }

                    WaferMaterial wafer;
                    if (!waferByInstance.TryGetValue(NormalizeId(slot.WaferInstanceId), out wafer) ||
                        !string.Equals(wafer.WaferId ?? "", slot.WaferId ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        reason = "Slot이 가리키는 Wafer Material이 일치하지 않습니다. cassette=" + cassette.Role +
                                 ", slot=" + (slot.SlotNumber + 1) +
                                 ", wafer=" + (slot.WaferId ?? "") +
                                 ", instance=" + (slot.WaferInstanceId ?? "");
                        return false;
                    }

                    MaterialLocationKind expectedKind = IsOutputCassetteRole(cassette.Role)
                        ? MaterialLocationKind.OutputCassette
                        : MaterialLocationKind.InputCassette;
                    WaferMaterialState waferState = WaferMaterialStateText.Normalize(wafer.State);
                    if (wafer.CurrentLocation == null ||
                        wafer.CurrentLocation.Kind != expectedKind ||
                        wafer.CurrentLocation.CassetteRole != cassette.Role ||
                        wafer.CurrentLocation.SlotNumber != slot.SlotNumber)
                    {
                        reason = "Slot pointer와 Wafer의 물리 위치가 일치하지 않습니다. cassette=" + cassette.Role +
                                  ", slot=" + (slot.SlotNumber + 1) +
                                  ", wafer=" + (wafer.WaferId ?? "");
                        return false;
                    }
                    if (slot.HasWafer == (waferState == WaferMaterialState.Empty))
                    {
                        reason = "Slot 점유 플래그와 Wafer 상태가 일치하지 않습니다. cassette=" + cassette.Role +
                                 ", slot=" + (slot.SlotNumber + 1) +
                                 ", wafer=" + (wafer.WaferId ?? "") +
                                 ", state=" + waferState;
                        return false;
                    }
                }
            }

            foreach (WaferMaterial wafer in snapshot.Wafers)
            {
                MaterialLocation location = wafer.CurrentLocation;
                if (location == null ||
                    (location.Kind != MaterialLocationKind.InputCassette &&
                     location.Kind != MaterialLocationKind.OutputCassette))
                {
                    continue;
                }

                CassetteMaterial cassette;
                if (!cassetteByRole.TryGetValue(location.CassetteRole, out cassette))
                {
                    reason = "Cassette 위치 Wafer의 Cassette Material이 없습니다. wafer=" +
                             (wafer.WaferId ?? "") + ", location=" + location;
                    return false;
                }
                bool expectedOutput = IsOutputCassetteRole(cassette.Role);
                if ((location.Kind == MaterialLocationKind.OutputCassette) != expectedOutput ||
                    location.SlotNumber < 0 ||
                    location.SlotNumber >= cassette.Slots.Count)
                {
                    reason = "Wafer의 Cassette 위치가 역할/슬롯 범위와 일치하지 않습니다. wafer=" +
                             (wafer.WaferId ?? "") + ", location=" + location;
                    return false;
                }

                CassetteSlotMaterial slot = cassette.Slots[location.SlotNumber];
                if (slot == null ||
                    !string.Equals(
                        NormalizeId(slot.WaferInstanceId),
                        NormalizeId(wafer.WaferInstanceId),
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        slot.WaferId ?? "",
                        wafer.WaferId ?? "",
                        StringComparison.OrdinalIgnoreCase))
                {
                    reason = "Cassette 위치 Wafer를 가리키는 역방향 Slot pointer가 없습니다. wafer=" +
                             (wafer.WaferId ?? "") + ", location=" + location;
                    return false;
                }
            }

            CassetteMaterialRole[] requiredRoles =
            {
                CassetteMaterialRole.Input1,
                CassetteMaterialRole.Input2,
                CassetteMaterialRole.Good1,
                CassetteMaterialRole.Good2,
                CassetteMaterialRole.Ng1
            };
            foreach (CassetteMaterialRole requiredRole in requiredRoles)
            {
                if (cassetteByRole.ContainsKey(requiredRole))
                    continue;
                reason = "필수 Cassette Material 역할이 없습니다. cassette=" + requiredRole;
                return false;
            }
            if (cassetteByRole.Count != requiredRoles.Length)
            {
                reason = "알 수 없는 Cassette Material 역할이 있습니다.";
                return false;
            }

            return true;
        }

        public static bool TryValidateRawMaterialValues(MaterialSnapshot snapshot, out string reason)
        {
            reason = "";
            if (snapshot == null || snapshot.Cassettes == null ||
                snapshot.Wafers == null || snapshot.Dies == null)
            {
                reason = "Material snapshot 원본 목록이 누락되었습니다.";
                return false;
            }

            foreach (CassetteMaterial cassette in snapshot.Cassettes)
            {
                if (cassette == null)
                    continue;
                if (!Enum.IsDefined(typeof(CassetteMaterialRole), cassette.Role))
                {
                    reason = "Cassette 역할 값이 올바르지 않습니다. role=" + (int)cassette.Role;
                    return false;
                }
            }

            foreach (WaferMaterial wafer in snapshot.Wafers)
            {
                if (wafer == null)
                    continue;

                int stateCode = (int)wafer.State;
                if (stateCode < 0 || stateCode > 7)
                {
                    reason = "Wafer 상태 값이 올바르지 않습니다. wafer=" + (wafer.WaferId ?? "") +
                             ", state=" + stateCode;
                    return false;
                }
                if (!Enum.IsDefined(typeof(CassetteMaterialRole), wafer.SourceCassetteRole) ||
                    !Enum.IsDefined(typeof(CassetteMaterialRole), wafer.OutputCassetteRole))
                {
                    reason = "Wafer Cassette 역할 값이 올바르지 않습니다. wafer=" + (wafer.WaferId ?? "");
                    return false;
                }
                if (!Enum.IsDefined(typeof(DieResult), wafer.OutputGrade))
                {
                    reason = "Wafer OutputGrade 값이 올바르지 않습니다. wafer=" + (wafer.WaferId ?? "");
                    return false;
                }
                if (!TryValidateRawLocation(wafer.CurrentLocation, "wafer=" + (wafer.WaferId ?? ""), out reason))
                    return false;
                if (IsPickerLocation(wafer.CurrentLocation.Kind))
                {
                    reason = "Wafer Material은 Picker 위치를 가질 수 없습니다. wafer=" +
                             (wafer.WaferId ?? "") + ", location=" + wafer.CurrentLocation;
                    return false;
                }
                if (wafer.DieIds == null || wafer.OutputReceiveSlots == null ||
                    wafer.InputStageRunReviewOrderedDieIds == null)
                {
                    reason = "Wafer Material의 필수 pointer 목록이 누락되었습니다. wafer=" +
                             (wafer.WaferId ?? "");
                    return false;
                }
                foreach (OutputReceiveSlotMaterial slot in wafer.OutputReceiveSlots)
                {
                    if (slot == null)
                    {
                        reason = "Output receive slot 목록에 null 항목이 있습니다. wafer=" +
                                 (wafer.WaferId ?? "");
                        return false;
                    }
                    if (!Enum.IsDefined(typeof(DieResult), slot.Result))
                    {
                        reason = "Output receive slot 결과 값이 올바르지 않습니다. wafer=" +
                                 (wafer.WaferId ?? "") + ", order=" + slot.OrderIndex;
                        return false;
                    }
                }
            }

            foreach (DieMaterial die in snapshot.Dies)
            {
                if (die == null)
                    continue;

                if (!TryValidateRawLocation(die.CurrentLocation, "die=" + (die.DieId ?? ""), out reason))
                    return false;
                if (!Enum.IsDefined(typeof(MaterialLocationKind), die.ReservedPickerLocation) ||
                    !Enum.IsDefined(typeof(MaterialLocationKind), die.PickedPickerLocation))
                {
                    reason = "Die Picker 위치 값이 올바르지 않습니다. die=" + (die.DieId ?? "");
                    return false;
                }
                if (!TryValidatePickerMarker(
                        die.ReservedPickerLocation,
                        die.ReservedPickerNo,
                        "Die 예약",
                        die.DieId,
                        out reason) ||
                    !TryValidatePickerMarker(
                        die.PickedPickerLocation,
                        die.PickedPickerNo,
                        "Die Pick 이력",
                        die.DieId,
                        out reason))
                {
                    return false;
                }
                bool hasPickedMarker = IsPickerLocation(die.PickedPickerLocation);
                bool hasPickedAt = die.PickedAt > new DateTime(1900, 1, 1, 23, 59, 59);
                if (hasPickedMarker != hasPickedAt)
                {
                    reason = "Die Pick 위치/번호/시각 이력이 서로 일치하지 않습니다. die=" +
                             (die.DieId ?? "") + ", pickedAt=" + die.PickedAt;
                    return false;
                }
                if (!Enum.IsDefined(typeof(DieResult), die.Result))
                {
                    reason = "Die 결과 값이 올바르지 않습니다. die=" + (die.DieId ?? "");
                    return false;
                }
                if (die.NgCodes == null || die.Inspections == null ||
                    die.WaferOffset == null || die.BinOffset == null)
                {
                    reason = "Die Material의 필수 검사 데이터가 누락되었습니다. die=" + (die.DieId ?? "");
                    return false;
                }
                foreach (DieInspectionRecord inspection in die.Inspections)
                {
                    if (inspection == null)
                    {
                        reason = "Die inspection 목록에 null 항목이 있습니다. die=" + (die.DieId ?? "");
                        return false;
                    }
                    if (!Enum.IsDefined(typeof(MaterialInspectionResult), inspection.Result))
                    {
                        reason = "Die inspection 결과 값이 올바르지 않습니다. die=" + (die.DieId ?? "");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TryValidateRawLocation(MaterialLocation location, string context, out string reason)
        {
            reason = "";
            if (location == null)
            {
                reason = "Material CurrentLocation이 누락되었습니다. " + (context ?? "");
                return false;
            }
            if (!Enum.IsDefined(typeof(MaterialLocationKind), location.Kind))
            {
                reason = "Material 위치 종류 값이 올바르지 않습니다. " + (context ?? "") +
                         ", kind=" + (int)location.Kind;
                return false;
            }
            if (!Enum.IsDefined(typeof(CassetteMaterialRole), location.CassetteRole))
            {
                reason = "Material 위치 Cassette 역할 값이 올바르지 않습니다. " + (context ?? "") +
                         ", role=" + (int)location.CassetteRole;
                return false;
            }
            if (IsPickerLocation(location.Kind))
            {
                if (!IsValidPickerNo(location.PickerNo))
                {
                    reason = "Picker 위치 번호가 올바르지 않습니다. " + (context ?? "") +
                             ", pickerNo=" + location.PickerNo;
                    return false;
                }
            }
            else if (location.PickerNo != -1)
            {
                reason = "Picker가 아닌 위치에 PickerNo가 남아 있습니다. " + (context ?? "") +
                         ", pickerNo=" + location.PickerNo;
                return false;
            }
            return true;
        }

        private static bool TryValidatePickerMarker(
            MaterialLocationKind location,
            int pickerNo,
            string context,
            string dieId,
            out string reason)
        {
            reason = "";
            if (location == MaterialLocationKind.Unknown)
            {
                if (pickerNo == -1)
                    return true;
                reason = context + " 위치는 비어 있지만 PickerNo가 남아 있습니다. die=" +
                         (dieId ?? "") + ", pickerNo=" + pickerNo;
                return false;
            }
            if (!IsPickerLocation(location) || !IsValidPickerNo(pickerNo))
            {
                reason = context + " 위치/PickerNo가 올바르지 않습니다. die=" +
                         (dieId ?? "") + ", location=" + location + ", pickerNo=" + pickerNo;
                return false;
            }
            return true;
        }

        private static bool ValidateMaterialParentPointers(
            MaterialSnapshot snapshot,
            IDictionary<string, WaferMaterial> waferByInstance,
            IDictionary<string, List<WaferMaterial>> wafersByDisplayId,
            out string reason)
        {
            reason = "";
            foreach (DieMaterial die in snapshot.Dies)
            {
                if (!ValidateWaferInstancePointer(
                    die.InputWaferInstanceId,
                    die.WaferID_Input,
                    "Die input parent, die=" + (die.DieId ?? ""),
                    waferByInstance,
                    wafersByDisplayId,
                    out reason))
                {
                    return false;
                }
                if (!ValidateWaferInstancePointer(
                    die.OutputWaferInstanceId,
                    die.WaferID_Output,
                    "Die output parent, die=" + (die.DieId ?? ""),
                    waferByInstance,
                    wafersByDisplayId,
                    out reason))
                {
                    return false;
                }

                bool hasInputParent =
                    !string.IsNullOrWhiteSpace(die.InputWaferInstanceId) ||
                    !string.IsNullOrWhiteSpace(die.WaferID_Input);
                bool hasOutputParent =
                    !string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) ||
                    !string.IsNullOrWhiteSpace(die.WaferID_Output);
                MaterialLocationKind locationKind = die.CurrentLocation != null
                    ? die.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                bool activeLocation = locationKind != MaterialLocationKind.Unknown &&
                                      locationKind != MaterialLocationKind.InputCassette &&
                                      locationKind != MaterialLocationKind.OutputCassette;
                bool reserved = IsPickerLocation(die.ReservedPickerLocation);
                if ((activeLocation || reserved) && !hasInputParent && !hasOutputParent)
                {
                    reason = "활성 위치/예약 Die에 Input 또는 Output Wafer parent가 없습니다. die=" +
                             (die.DieId ?? "") + ", location=" + locationKind;
                    return false;
                }
            }

            foreach (WaferMaterial wafer in snapshot.Wafers)
            {
                if (!ValidateWaferInstancePointer(
                    wafer.OutputReceiveSourceWaferInstanceId,
                    wafer.OutputReceiveSourceWaferId,
                    "Output receive source, wafer=" + (wafer.WaferId ?? ""),
                    waferByInstance,
                    wafersByDisplayId,
                    out reason))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateWaferInstancePointer(
            string instanceId,
            string displayId,
            string context,
            IDictionary<string, WaferMaterial> waferByInstance,
            IDictionary<string, List<WaferMaterial>> wafersByDisplayId,
            out string reason)
        {
            reason = "";
            string normalizedInstance = NormalizeId(instanceId);
            string normalizedDisplay = NormalizeId(displayId);
            if (string.IsNullOrWhiteSpace(normalizedInstance))
            {
                if (string.IsNullOrWhiteSpace(normalizedDisplay))
                    return true;

                List<WaferMaterial> legacyCandidates;
                int candidateCount = wafersByDisplayId.TryGetValue(normalizedDisplay, out legacyCandidates)
                    ? legacyCandidates.Count
                    : 0;
                if (candidateCount != 1)
                {
                    reason = "Legacy Wafer pointer 대상을 하나로 확인할 수 없습니다. context=" + context +
                             ", wafer=" + normalizedDisplay +
                             ", candidates=" + candidateCount;
                    return false;
                }
                return true;
            }

            WaferMaterial wafer;
            if (!waferByInstance.TryGetValue(normalizedInstance, out wafer))
            {
                reason = "Wafer instance pointer 대상이 없습니다. context=" + context +
                         ", instance=" + normalizedInstance;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(normalizedDisplay) &&
                !string.Equals(wafer.WaferId ?? "", normalizedDisplay, StringComparison.OrdinalIgnoreCase))
            {
                reason = "Wafer instance/display pointer가 일치하지 않습니다. context=" + context +
                         ", instance=" + normalizedInstance +
                         ", pointerWafer=" + normalizedDisplay +
                         ", actualWafer=" + (wafer.WaferId ?? "");
                return false;
            }

            return true;
        }

        private static bool ValidateWaferDiePointers(
            MaterialSnapshot snapshot,
            IDictionary<string, DieMaterial> dieById,
            IDictionary<string, WaferMaterial> waferByInstance,
            IDictionary<string, List<WaferMaterial>> wafersByDisplayId,
            out string reason)
        {
            reason = "";
            foreach (WaferMaterial wafer in snapshot.Wafers)
            {
                if (wafer.DieIds != null)
                {
                    var waferDieIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string rawDieId in wafer.DieIds)
                    {
                        string dieId = NormalizeId(rawDieId);
                        DieMaterial die;
                        if (string.IsNullOrWhiteSpace(dieId) || !dieById.TryGetValue(dieId, out die))
                        {
                            reason = "Wafer.DieIds가 존재하지 않는 Die를 가리킵니다. wafer=" +
                                     (wafer.WaferId ?? "") + ", die=" + (rawDieId ?? "");
                            return false;
                        }
                        if (!waferDieIds.Add(dieId))
                        {
                            reason = "Wafer.DieIds에 중복 Die가 있습니다. wafer=" +
                                     (wafer.WaferId ?? "") + ", die=" + dieId;
                            return false;
                        }
                        if (!DoesDieReferenceWafer(
                            die,
                            wafer,
                            true,
                            true,
                            waferByInstance,
                            wafersByDisplayId))
                        {
                            reason = "Wafer.DieIds의 Die parent가 Wafer와 일치하지 않습니다. wafer=" +
                                     (wafer.WaferId ?? "") + ", die=" + dieId;
                            return false;
                        }
                    }
                }

                string reviewStartDieId = NormalizeId(wafer.InputStageRunReviewStartDieUid);
                DieMaterial reviewStartDie;
                if (!string.IsNullOrWhiteSpace(reviewStartDieId) &&
                    (!dieById.TryGetValue(reviewStartDieId, out reviewStartDie) ||
                     !DoesDieReferenceWafer(
                         reviewStartDie,
                         wafer,
                         true,
                         false,
                         waferByInstance,
                         wafersByDisplayId)))
                {
                    reason = "Input Stage review 시작 pointer가 해당 Input Wafer의 Die를 가리키지 않습니다. wafer=" +
                             (wafer.WaferId ?? "") + ", die=" + reviewStartDieId;
                    return false;
                }
                if (wafer.InputStageRunReviewOrderedDieIds != null)
                {
                    var reviewIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string rawDieId in wafer.InputStageRunReviewOrderedDieIds)
                    {
                        string dieId = NormalizeId(rawDieId);
                        DieMaterial reviewDie;
                        if (string.IsNullOrWhiteSpace(dieId) ||
                            !dieById.TryGetValue(dieId, out reviewDie) ||
                            !DoesDieReferenceWafer(
                                reviewDie,
                                wafer,
                                true,
                                false,
                                waferByInstance,
                                wafersByDisplayId))
                        {
                            reason = "Input Stage review 순서가 해당 Input Wafer의 Die를 가리키지 않습니다. wafer=" +
                                     (wafer.WaferId ?? "") + ", die=" + (rawDieId ?? "");
                            return false;
                        }
                        if (!reviewIds.Add(dieId))
                        {
                            reason = "Input Stage review 순서에 중복 Die가 있습니다. wafer=" +
                                     (wafer.WaferId ?? "") + ", die=" + dieId;
                            return false;
                        }
                    }
                }

                if (wafer.OutputReceiveSlots == null)
                    continue;

                var receiveOrderIndexes = new HashSet<int>();
                var placedSourceDieIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (OutputReceiveSlotMaterial slot in wafer.OutputReceiveSlots)
                {
                    if (slot == null)
                    {
                        reason = "Output receive slot 목록에 null 항목이 있습니다. wafer=" +
                                 (wafer.WaferId ?? "");
                        return false;
                    }
                    if (slot.OrderIndex < 0 || !receiveOrderIndexes.Add(slot.OrderIndex))
                    {
                        reason = "Output receive OrderIndex가 음수이거나 중복되었습니다. wafer=" +
                                 (wafer.WaferId ?? "") + ", order=" + slot.OrderIndex;
                        return false;
                    }

                    string sourceDieId = NormalizeId(slot.SourceDieUid);
                    string dieId = NormalizeId(slot.DieUid);
                    if (!string.IsNullOrWhiteSpace(sourceDieId))
                    {
                        if (string.IsNullOrWhiteSpace(dieId) ||
                            !string.Equals(sourceDieId, dieId, StringComparison.OrdinalIgnoreCase))
                        {
                            reason = "Place 완료 Output receive slot의 DieUid/SourceDieUid가 일치하지 않습니다. wafer=" +
                                     (wafer.WaferId ?? "") + ", order=" + slot.OrderIndex;
                            return false;
                        }
                        if (!placedSourceDieIds.Add(sourceDieId))
                        {
                            reason = "같은 Source Die가 Output receive slot 둘 이상에 배치되었습니다. wafer=" +
                                     (wafer.WaferId ?? "") + ", die=" + sourceDieId;
                            return false;
                        }
                    }
                    DieMaterial sourceDie;
                    if (!string.IsNullOrWhiteSpace(sourceDieId) &&
                        (!dieById.TryGetValue(sourceDieId, out sourceDie) ||
                         !DoesDieReferenceWafer(
                             sourceDie,
                             wafer,
                             false,
                             true,
                             waferByInstance,
                             wafersByDisplayId)))
                    {
                        reason = "Output receive source slot이 해당 Output Wafer의 Die를 가리키지 않습니다. wafer=" +
                                 (wafer.WaferId ?? "") + ", die=" + sourceDieId;
                        return false;
                    }

                    // SourceDieUid가 실제 Place 완료 binding의 기준이다. Source가 비어 있는
                    // 계획 slot의 DieUid는 수동 Map Transfer 화면용 cell UID일 수 있다.
                    DieMaterial receiveDie;
                    if (!string.IsNullOrWhiteSpace(sourceDieId) &&
                        !string.IsNullOrWhiteSpace(dieId) &&
                        (!dieById.TryGetValue(dieId, out receiveDie) ||
                         !DoesDieReferenceWafer(
                             receiveDie,
                             wafer,
                             false,
                             true,
                             waferByInstance,
                             wafersByDisplayId)))
                    {
                        reason = "Place 완료 Output receive slot이 해당 Output Wafer의 Die를 가리키지 않습니다. wafer=" +
                                 (wafer.WaferId ?? "") + ", die=" + dieId;
                        return false;
                    }
                }
                for (int orderIndex = 0; orderIndex < wafer.OutputReceiveSlots.Count; orderIndex++)
                {
                    if (receiveOrderIndexes.Contains(orderIndex))
                        continue;
                    reason = "Output receive OrderIndex가 0부터 연속되지 않습니다. wafer=" +
                             (wafer.WaferId ?? "") + ", missingOrder=" + orderIndex;
                    return false;
                }
            }

            return true;
        }

        private static bool DoesDieReferenceWafer(
            DieMaterial die,
            WaferMaterial wafer,
            bool allowInputParent,
            bool allowOutputParent,
            IDictionary<string, WaferMaterial> waferByInstance,
            IDictionary<string, List<WaferMaterial>> wafersByDisplayId)
        {
            if (die == null || wafer == null)
                return false;

            if (allowInputParent && DoesWaferPointerReference(
                die.InputWaferInstanceId,
                die.WaferID_Input,
                wafer,
                waferByInstance,
                wafersByDisplayId))
            {
                return true;
            }

            return allowOutputParent && DoesWaferPointerReference(
                die.OutputWaferInstanceId,
                die.WaferID_Output,
                wafer,
                waferByInstance,
                wafersByDisplayId);
        }

        private static bool DoesWaferPointerReference(
            string instanceId,
            string displayId,
            WaferMaterial wafer,
            IDictionary<string, WaferMaterial> waferByInstance,
            IDictionary<string, List<WaferMaterial>> wafersByDisplayId)
        {
            string normalizedInstance = NormalizeId(instanceId);
            if (!string.IsNullOrWhiteSpace(normalizedInstance))
            {
                WaferMaterial parent;
                return waferByInstance.TryGetValue(normalizedInstance, out parent) &&
                       object.ReferenceEquals(parent, wafer);
            }

            string normalizedDisplay = NormalizeId(displayId);
            List<WaferMaterial> candidates;
            return !string.IsNullOrWhiteSpace(normalizedDisplay) &&
                   wafersByDisplayId.TryGetValue(normalizedDisplay, out candidates) &&
                   candidates.Count == 1 &&
                   object.ReferenceEquals(candidates[0], wafer);
        }

        private static bool ValidateActiveLocations(MaterialSnapshot snapshot, out string reason)
        {
            reason = "";
            var waferLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (WaferMaterial wafer in snapshot.Wafers)
            {
                string locationKey = BuildWaferLocationKey(wafer.CurrentLocation);
                if (string.IsNullOrWhiteSpace(locationKey))
                    continue;
                if (!waferLocations.Add(locationKey))
                {
                    reason = "같은 물리 위치에 Wafer Material이 둘 이상 있습니다. location=" + locationKey;
                    return false;
                }
            }

            var pickerLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pickerReservations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DieMaterial die in snapshot.Dies)
            {
                MaterialLocation location = die.CurrentLocation;
                if (location != null && IsPickerLocation(location.Kind))
                {
                    if (!IsValidPickerNo(location.PickerNo))
                    {
                        reason = "Picker 위치 Die의 PickerNo가 올바르지 않습니다. die=" + (die.DieId ?? "");
                        return false;
                    }

                    string key = location.Kind + ":" + location.PickerNo;
                    if (!pickerLocations.Add(key))
                    {
                        reason = "같은 Picker head에 Die Material이 둘 이상 있습니다. picker=" + key;
                        return false;
                    }
                }

                bool hasReservedLocation = IsPickerLocation(die.ReservedPickerLocation);
                bool hasReservedNo = IsValidPickerNo(die.ReservedPickerNo);
                if (hasReservedLocation != hasReservedNo)
                {
                    reason = "Die 예약 위치와 PickerNo가 일치하지 않습니다. die=" + (die.DieId ?? "");
                    return false;
                }
                if (!hasReservedLocation && die.ReservedPickerNo != -1)
                {
                    reason = "Die 예약이 없지만 PickerNo가 남아 있습니다. die=" + (die.DieId ?? "");
                    return false;
                }
                if (hasReservedLocation)
                {
                    string key = die.ReservedPickerLocation + ":" + die.ReservedPickerNo;
                    if (!pickerReservations.Add(key))
                    {
                        reason = "같은 Picker head에 Die 예약이 둘 이상 있습니다. picker=" + key;
                        return false;
                    }
                }
            }

            return true;
        }

        private static string BuildWaferLocationKey(MaterialLocation location)
        {
            if (location == null || location.Kind == MaterialLocationKind.Unknown)
                return "";
            if (location.Kind == MaterialLocationKind.InputCassette ||
                location.Kind == MaterialLocationKind.OutputCassette)
            {
                return location.Kind + ":" + location.CassetteRole + ":" + location.SlotNumber;
            }
            if (IsPickerLocation(location.Kind))
                return location.Kind + ":" + location.PickerNo;
            return location.Kind.ToString();
        }

        private static bool IsOutputCassetteRole(CassetteMaterialRole role)
        {
            return role == CassetteMaterialRole.Good1 ||
                   role == CassetteMaterialRole.Good2 ||
                   role == CassetteMaterialRole.Ng1;
        }

        private static bool IsPickerLocation(MaterialLocationKind kind)
        {
            return kind == MaterialLocationKind.PickerFront ||
                   kind == MaterialLocationKind.PickerRear;
        }

        private static bool IsValidPickerNo(int pickerNo)
        {
            return pickerNo >= MinimumPickerNo && pickerNo <= MaximumPickerNo;
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        }

        private sealed class MaterialGraphContext
        {
            private readonly MaterialSnapshot _snapshot;
            private readonly MaterialCompactionResult _result;
            private readonly HashSet<WaferMaterial> _retainedWafers = new HashSet<WaferMaterial>();
            private readonly HashSet<DieMaterial> _retainedDies = new HashSet<DieMaterial>();
            private readonly Queue<WaferMaterial> _waferQueue = new Queue<WaferMaterial>();
            private readonly Queue<DieMaterial> _dieQueue = new Queue<DieMaterial>();
            private readonly Dictionary<string, List<WaferMaterial>> _wafersByInstance =
                new Dictionary<string, List<WaferMaterial>>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, List<WaferMaterial>> _wafersByDisplayId =
                new Dictionary<string, List<WaferMaterial>>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, List<DieMaterial>> _diesById =
                new Dictionary<string, List<DieMaterial>>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, List<DieMaterial>> _diesByInputInstance =
                new Dictionary<string, List<DieMaterial>>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, List<DieMaterial>> _diesByOutputInstance =
                new Dictionary<string, List<DieMaterial>>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, List<DieMaterial>> _legacyDiesByInputDisplayId =
                new Dictionary<string, List<DieMaterial>>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, List<DieMaterial>> _legacyDiesByOutputDisplayId =
                new Dictionary<string, List<DieMaterial>>(StringComparer.OrdinalIgnoreCase);

            public MaterialGraphContext(MaterialSnapshot snapshot, MaterialCompactionResult result)
            {
                _snapshot = snapshot;
                _result = result;
                BuildIndexes();
            }

            public void SeedRoots(IEnumerable<string> externalDieRootIds)
            {
                foreach (WaferMaterial wafer in _snapshot.Wafers)
                {
                    if (wafer == null)
                        continue;

                    MaterialLocationKind locationKind = wafer.CurrentLocation != null
                        ? wafer.CurrentLocation.Kind
                        : MaterialLocationKind.Unknown;
                    if (WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty ||
                        locationKind != MaterialLocationKind.Unknown)
                    {
                        RetainWafer(wafer);
                    }
                }

                foreach (CassetteMaterial cassette in _snapshot.Cassettes)
                {
                    if (cassette == null || cassette.Slots == null)
                        continue;

                    foreach (CassetteSlotMaterial slot in cassette.Slots)
                    {
                        if (slot != null &&
                            (slot.HasWafer ||
                             !string.IsNullOrWhiteSpace(slot.WaferId) ||
                             !string.IsNullOrWhiteSpace(slot.WaferInstanceId)))
                        {
                            RetainWaferReference(
                                slot.WaferInstanceId,
                                slot.WaferId,
                                "cassette=" + cassette.Role + ", slot=" + (slot.SlotNumber + 1));
                        }
                    }
                }

                foreach (DieMaterial die in _snapshot.Dies)
                {
                    if (die == null)
                        continue;

                    MaterialLocationKind locationKind = die.CurrentLocation != null
                        ? die.CurrentLocation.Kind
                        : MaterialLocationKind.Unknown;
                    bool activeLocation = locationKind != MaterialLocationKind.Unknown &&
                                          locationKind != MaterialLocationKind.InputCassette &&
                                          locationKind != MaterialLocationKind.OutputCassette;
                    bool reserved = IsPickerLocation(die.ReservedPickerLocation) || die.ReservedPickerNo > 0;
                    if (activeLocation || reserved)
                        RetainDie(die);
                }

                if (externalDieRootIds == null)
                    return;

                foreach (string dieId in externalDieRootIds)
                    RetainDieReference(dieId, "active input map");
            }

            public void ResolveReachableGraph()
            {
                while (_waferQueue.Count > 0 || _dieQueue.Count > 0)
                {
                    while (_waferQueue.Count > 0)
                        ResolveWafer(_waferQueue.Dequeue());
                    while (_dieQueue.Count > 0)
                        ResolveDie(_dieQueue.Dequeue());
                }
            }

            public void RemoveUnreachableMaterials()
            {
                var removedDieIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _result.RemovedDieCount = _snapshot.Dies.RemoveAll(die =>
                {
                    if (die != null && _retainedDies.Contains(die))
                        return false;
                    if (die != null && !IsPassiveDie(die))
                    {
                        _result.AddWarning(
                            "도달 불가능하지만 활성 위치/예약 정보가 있는 Die를 보존합니다. die=" +
                            (die.DieId ?? ""));
                        return false;
                    }
                    if (die != null && !string.IsNullOrWhiteSpace(die.DieId))
                        removedDieIds.Add(die.DieId.Trim());
                    return true;
                });

                _result.RemovedWaferCount = _snapshot.Wafers.RemoveAll(wafer =>
                    wafer == null ||
                    (!_retainedWafers.Contains(wafer) &&
                     WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty &&
                     (wafer.CurrentLocation == null || wafer.CurrentLocation.Kind == MaterialLocationKind.Unknown)));

                if (removedDieIds.Count == 0)
                    return;

                foreach (WaferMaterial wafer in _snapshot.Wafers)
                {
                    if (wafer != null && wafer.DieIds != null)
                    {
                        wafer.DieIds.RemoveAll(id =>
                            !string.IsNullOrWhiteSpace(id) && removedDieIds.Contains(id.Trim()));
                    }
                }
            }

            private void BuildIndexes()
            {
                foreach (WaferMaterial wafer in _snapshot.Wafers)
                {
                    if (wafer == null)
                        continue;
                    AddIndex(_wafersByInstance, wafer.WaferInstanceId, wafer);
                    AddIndex(_wafersByDisplayId, wafer.WaferId, wafer);
                }

                foreach (KeyValuePair<string, List<WaferMaterial>> item in _wafersByInstance)
                {
                    if (item.Value.Count > 1)
                    {
                        _result.AddWarning("WaferInstanceId가 중복되어 관련 Material을 모두 보존합니다. instance=" + item.Key);
                        foreach (WaferMaterial wafer in item.Value)
                            RetainWafer(wafer);
                    }
                }

                foreach (DieMaterial die in _snapshot.Dies)
                {
                    if (die == null)
                        continue;

                    AddIndex(_diesById, die.DieId, die);
                    AddIndex(_diesByInputInstance, die.InputWaferInstanceId, die);
                    AddIndex(_diesByOutputInstance, die.OutputWaferInstanceId, die);
                    if (string.IsNullOrWhiteSpace(die.InputWaferInstanceId))
                        AddIndex(_legacyDiesByInputDisplayId, die.WaferID_Input, die);
                    if (string.IsNullOrWhiteSpace(die.OutputWaferInstanceId))
                        AddIndex(_legacyDiesByOutputDisplayId, die.WaferID_Output, die);
                }

                foreach (KeyValuePair<string, List<DieMaterial>> item in _diesById)
                {
                    if (item.Value.Count > 1)
                    {
                        _result.AddWarning("DieId가 중복되어 관련 Material을 모두 보존합니다. die=" + item.Key);
                        foreach (DieMaterial die in item.Value)
                            RetainDie(die);
                    }
                }
            }

            private void ResolveWafer(WaferMaterial wafer)
            {
                RetainOwnedWaferDieReferences(wafer);
                RetainDieReference(wafer.InputStageRunReviewStartDieUid, "input review start die");
                RetainDieReferences(wafer.InputStageRunReviewOrderedDieIds, "input review order");

                if (wafer.OutputReceiveSlots != null)
                {
                    foreach (OutputReceiveSlotMaterial slot in wafer.OutputReceiveSlots)
                    {
                        if (slot == null)
                            continue;
                        if (!string.IsNullOrWhiteSpace(slot.SourceDieUid))
                        {
                            RetainDieReference(slot.SourceDieUid, "output receive source slot");
                            RetainDieReference(slot.DieUid, "output receive placed slot");
                        }
                        else
                        {
                            // Legacy snapshot은 SourceDieUid가 없고 DieUid만 실제 물리 Die를
                            // 가리킬 수 있다. 반면 수동 Output Map의 DieUid는 State.Dies에 없는
                            // cell UID일 수 있으므로, 실제 Die가 존재할 때만 조용히 root로 보존한다.
                            string legacyDieId = NormalizeId(slot.DieUid);
                            List<DieMaterial> legacyDies;
                            if (!string.IsNullOrWhiteSpace(legacyDieId) &&
                                _diesById.TryGetValue(legacyDieId, out legacyDies))
                            {
                                foreach (DieMaterial legacyDie in legacyDies)
                                    RetainDie(legacyDie);
                            }
                        }
                    }
                }

                RetainWaferReference(
                    wafer.OutputReceiveSourceWaferInstanceId,
                    wafer.OutputReceiveSourceWaferId,
                    "output source wafer=" + (wafer.WaferId ?? ""));

                string instanceId = NormalizeId(wafer.WaferInstanceId);
                if (!string.IsNullOrWhiteSpace(instanceId))
                {
                    RetainIndexedDies(_diesByInputInstance, instanceId);
                    RetainIndexedDies(_diesByOutputInstance, instanceId);
                }

                // Legacy Die는 parent instance가 비어 있고 표시 ID만 갖는다. 시작 시 Wafer에
                // instance를 부여한 뒤에도 이 역참조를 따라야 기존 진행 Material이 유실되지 않는다.
                RetainIndexedDies(_legacyDiesByInputDisplayId, wafer.WaferId);
                RetainIndexedDies(_legacyDiesByOutputDisplayId, wafer.WaferId);
            }

            private void RetainOwnedWaferDieReferences(WaferMaterial wafer)
            {
                if (wafer == null || wafer.DieIds == null)
                    return;

                var normalizedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var retainedIds = new List<string>();
                bool changed = false;
                foreach (string rawDieId in wafer.DieIds)
                {
                    string dieId = NormalizeId(rawDieId);
                    if (string.IsNullOrWhiteSpace(dieId))
                    {
                        retainedIds.Add(rawDieId);
                        continue;
                    }

                    if (!normalizedIds.Add(dieId))
                    {
                        changed = true;
                        _result.AddWarning("Wafer.DieIds의 중복 pointer를 정리합니다. wafer=" +
                                           (wafer.WaferId ?? "") + ", die=" + dieId);
                        continue;
                    }

                    List<DieMaterial> dies;
                    if (!_diesById.TryGetValue(dieId, out dies) || dies.Count != 1)
                    {
                        // 대상 유실/중복은 validator가 저장을 차단한다. 여기서는 임의 삭제하지 않는다.
                        retainedIds.Add(rawDieId);
                        RetainDieReference(dieId, "wafer.DieIds, wafer=" + (wafer.WaferId ?? ""));
                        continue;
                    }

                    DieMaterial die = dies[0];
                    if (!DoesIndexedDieReferenceWafer(die, wafer, true, true))
                    {
                        if (HasOnlyConsistentDifferentWaferParents(die, wafer))
                        {
                            changed = true;
                            _result.AddWarning("다른 Wafer parent를 가진 stale Wafer.DieIds pointer를 정리합니다. wafer=" +
                                               (wafer.WaferId ?? "") + ", die=" + dieId);
                            continue;
                        }

                        // parent가 누락/불일치한 경우 pointer를 지워 손상을 숨기지 않는다.
                        // Die를 보존한 채 validator가 저장과 복구 후보 채택을 명시적으로 차단한다.
                        retainedIds.Add(rawDieId);
                        RetainDie(die);
                        _result.AddWarning("Wafer.DieIds의 Die parent를 안전하게 결정할 수 없어 보존합니다. wafer=" +
                                           (wafer.WaferId ?? "") + ", die=" + dieId);
                        continue;
                    }

                    retainedIds.Add(rawDieId);
                    RetainDie(die);
                }

                if (changed)
                    wafer.DieIds = retainedIds;
            }

            private bool DoesIndexedDieReferenceWafer(
                DieMaterial die,
                WaferMaterial wafer,
                bool allowInputParent,
                bool allowOutputParent)
            {
                if (die == null || wafer == null)
                    return false;

                if (allowInputParent && DoesIndexedWaferPointerReference(
                    die.InputWaferInstanceId,
                    die.WaferID_Input,
                    wafer))
                {
                    return true;
                }

                return allowOutputParent && DoesIndexedWaferPointerReference(
                    die.OutputWaferInstanceId,
                    die.WaferID_Output,
                    wafer);
            }

            private bool HasOnlyConsistentDifferentWaferParents(
                DieMaterial die,
                WaferMaterial referencedWafer)
            {
                if (die == null || referencedWafer == null)
                    return false;

                bool hasResolvedParent = false;
                WaferMaterial inputParent;
                bool hasInputPointer;
                if (!TryResolveConsistentWaferParent(
                    die.InputWaferInstanceId,
                    die.WaferID_Input,
                    out hasInputPointer,
                    out inputParent))
                {
                    return false;
                }
                if (hasInputPointer)
                {
                    hasResolvedParent = true;
                    if (object.ReferenceEquals(inputParent, referencedWafer))
                        return false;
                }

                WaferMaterial outputParent;
                bool hasOutputPointer;
                if (!TryResolveConsistentWaferParent(
                    die.OutputWaferInstanceId,
                    die.WaferID_Output,
                    out hasOutputPointer,
                    out outputParent))
                {
                    return false;
                }
                if (hasOutputPointer)
                {
                    hasResolvedParent = true;
                    if (object.ReferenceEquals(outputParent, referencedWafer))
                        return false;
                }

                return hasResolvedParent;
            }

            private bool TryResolveConsistentWaferParent(
                string instanceId,
                string displayId,
                out bool hasPointer,
                out WaferMaterial wafer)
            {
                wafer = null;
                string normalizedInstance = NormalizeId(instanceId);
                string normalizedDisplay = NormalizeId(displayId);
                hasPointer = !string.IsNullOrWhiteSpace(normalizedInstance) ||
                             !string.IsNullOrWhiteSpace(normalizedDisplay);
                if (!hasPointer)
                    return true;

                List<WaferMaterial> candidates;
                if (!string.IsNullOrWhiteSpace(normalizedInstance))
                {
                    if (!_wafersByInstance.TryGetValue(normalizedInstance, out candidates) ||
                        candidates.Count != 1)
                    {
                        return false;
                    }

                    wafer = candidates[0];
                    return string.IsNullOrWhiteSpace(normalizedDisplay) ||
                           string.Equals(
                               wafer.WaferId ?? "",
                               normalizedDisplay,
                               StringComparison.OrdinalIgnoreCase);
                }

                if (!_wafersByDisplayId.TryGetValue(normalizedDisplay, out candidates) ||
                    candidates.Count != 1)
                {
                    return false;
                }

                wafer = candidates[0];
                return true;
            }

            private bool DoesIndexedWaferPointerReference(
                string instanceId,
                string displayId,
                WaferMaterial wafer)
            {
                string normalizedInstance = NormalizeId(instanceId);
                if (!string.IsNullOrWhiteSpace(normalizedInstance))
                {
                    List<WaferMaterial> instanceCandidates;
                    return _wafersByInstance.TryGetValue(normalizedInstance, out instanceCandidates) &&
                           instanceCandidates.Count == 1 &&
                           object.ReferenceEquals(instanceCandidates[0], wafer);
                }

                string normalizedDisplay = NormalizeId(displayId);
                List<WaferMaterial> displayCandidates;
                return !string.IsNullOrWhiteSpace(normalizedDisplay) &&
                       _wafersByDisplayId.TryGetValue(normalizedDisplay, out displayCandidates) &&
                       displayCandidates.Count == 1 &&
                       object.ReferenceEquals(displayCandidates[0], wafer);
            }

            private void ResolveDie(DieMaterial die)
            {
                RetainWaferReference(
                    die.InputWaferInstanceId,
                    die.WaferID_Input,
                    "input parent of die=" + (die.DieId ?? ""));
                RetainWaferReference(
                    die.OutputWaferInstanceId,
                    die.WaferID_Output,
                    "output parent of die=" + (die.DieId ?? ""));
            }

            private void RetainWaferReference(string instanceId, string displayId, string context)
            {
                string normalizedInstance = NormalizeId(instanceId);
                string normalizedDisplay = NormalizeId(displayId);
                if (string.IsNullOrWhiteSpace(normalizedInstance) &&
                    string.IsNullOrWhiteSpace(normalizedDisplay))
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(normalizedInstance))
                {
                    int instanceCount = RetainIndexedWafers(_wafersByInstance, normalizedInstance);
                    List<WaferMaterial> instanceWafers;
                    if (instanceCount == 0)
                    {
                        _result.AddWarning("Wafer instance pointer 대상이 없습니다. " +
                                           context + ", instance=" + normalizedInstance +
                                           ", wafer=" + normalizedDisplay);
                        RetainIndexedWafers(_wafersByDisplayId, normalizedDisplay);
                    }
                    else if (!string.IsNullOrWhiteSpace(normalizedDisplay) &&
                        _wafersByInstance.TryGetValue(normalizedInstance, out instanceWafers) &&
                        instanceWafers.Any(w => !string.Equals(w.WaferId ?? "", normalizedDisplay, StringComparison.OrdinalIgnoreCase)))
                    {
                        _result.AddWarning("Wafer instance/display 관계가 일치하지 않습니다. " + context);
                        RetainIndexedWafers(_wafersByDisplayId, normalizedDisplay);
                    }
                    return;
                }

                int displayCount = RetainIndexedWafers(_wafersByDisplayId, normalizedDisplay);
                if (displayCount != 1)
                {
                    _result.AddWarning("Legacy Wafer pointer를 하나로 결정할 수 없어 가능한 Material을 보존합니다. " +
                                       context + ", wafer=" + normalizedDisplay +
                                       ", candidates=" + displayCount);
                }
            }

            private void RetainDieReferences(IEnumerable<string> dieIds, string context)
            {
                if (dieIds == null)
                    return;
                foreach (string dieId in dieIds)
                    RetainDieReference(dieId, context);
            }

            private void RetainDieReference(string dieId, string context)
            {
                string normalized = NormalizeId(dieId);
                if (string.IsNullOrWhiteSpace(normalized))
                    return;

                List<DieMaterial> dies;
                if (!_diesById.TryGetValue(normalized, out dies) || dies.Count == 0)
                {
                    _result.AddWarning("Die pointer 대상이 없어 기존 pointer를 유지합니다. context=" +
                                       context + ", die=" + normalized);
                    return;
                }

                foreach (DieMaterial die in dies)
                    RetainDie(die);
            }

            private int RetainIndexedWafers(
                IDictionary<string, List<WaferMaterial>> index,
                string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return 0;

                List<WaferMaterial> wafers;
                if (!index.TryGetValue(key.Trim(), out wafers))
                    return 0;

                foreach (WaferMaterial wafer in wafers)
                    RetainWafer(wafer);
                return wafers.Count;
            }

            private void RetainIndexedDies(
                IDictionary<string, List<DieMaterial>> index,
                string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return;

                List<DieMaterial> dies;
                if (!index.TryGetValue(key.Trim(), out dies))
                    return;

                foreach (DieMaterial die in dies)
                    RetainDie(die);
            }

            private void RetainWafer(WaferMaterial wafer)
            {
                if (wafer != null && _retainedWafers.Add(wafer))
                    _waferQueue.Enqueue(wafer);
            }

            private void RetainDie(DieMaterial die)
            {
                if (die != null && _retainedDies.Add(die))
                    _dieQueue.Enqueue(die);
            }

            private static bool IsPassiveDie(DieMaterial die)
            {
                if (die == null)
                    return true;

                MaterialLocationKind locationKind = die.CurrentLocation != null
                    ? die.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                bool passiveLocation = locationKind == MaterialLocationKind.Unknown ||
                                       locationKind == MaterialLocationKind.InputCassette ||
                                       locationKind == MaterialLocationKind.OutputCassette;
                bool hasReservation = IsPickerLocation(die.ReservedPickerLocation) ||
                                      die.ReservedPickerNo > 0;
                return passiveLocation && !hasReservation;
            }

            private static void AddIndex<T>(
                IDictionary<string, List<T>> index,
                string key,
                T value)
            {
                string normalized = NormalizeId(key);
                if (string.IsNullOrWhiteSpace(normalized) || value == null)
                    return;

                List<T> values;
                if (!index.TryGetValue(normalized, out values))
                {
                    values = new List<T>();
                    index.Add(normalized, values);
                }
                values.Add(value);
            }
        }
    }
}
