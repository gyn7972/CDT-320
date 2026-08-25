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
    // MaterialStateService partial: 카세트/위치 기반 Wafer 배치·조회·검증 (원본 2170-2796)
    public static partial class MaterialStateService
    {
        public static void PutWaferInCassette(
            string waferId,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition = double.NaN)
        {
            PutWaferInCassette(null, waferId, cassetteRole, slotNumber, cassetteLotId, slotPosition, false, WaferMaterialState.Ready);
        }

        public static void PutWaferInCassette(
            WaferMaterial wafer,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition = double.NaN)
        {
            PutWaferInCassette(
                wafer,
                wafer != null ? wafer.WaferId : "",
                cassetteRole,
                slotNumber,
                cassetteLotId,
                slotPosition,
                false,
                WaferMaterialState.Ready);
        }

        public static void PutWaferInCassette(
            string waferId,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition,
            WaferMaterialState state)
        {
            PutWaferInCassette(null, waferId, cassetteRole, slotNumber, cassetteLotId, slotPosition, true, state);
        }

        public static void PutWaferInCassette(
            WaferMaterial wafer,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition,
            WaferMaterialState state)
        {
            PutWaferInCassette(
                wafer,
                wafer != null ? wafer.WaferId : "",
                cassetteRole,
                slotNumber,
                cassetteLotId,
                slotPosition,
                true,
                state);
        }

        private static void PutWaferInCassette(
            WaferMaterial requestedWafer,
            string waferId,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition,
            bool updateState,
            WaferMaterialState state)
        {
            lock (_stateSync)
            {
                if (string.IsNullOrWhiteSpace(waferId))
                    throw new InvalidOperationException("Cassette에 저장할 Wafer/Bin ID가 없습니다.");

                var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
                if (cassette == null || !cassette.IsEnabled || !cassette.IsPresent || !cassette.IsMapped)
                    throw new InvalidOperationException("대상 cassette가 활성화 또는 mapping 상태가 아닙니다. cassette=" + cassetteRole);

                cassette.EnsureSlots();
                if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                    throw new ArgumentOutOfRangeException("slotNumber", "Cassette slot 범위를 벗어났습니다. cassette=" + cassetteRole + ", slot=" + (slotNumber + 1));

                CassetteSlotMaterial targetSlot = cassette.Slots[slotNumber];
                if (targetSlot == null)
                    throw new InvalidOperationException("대상 cassette slot 데이터가 없습니다. cassette=" + cassetteRole + ", slot=" + (slotNumber + 1));

                bool targetHasWaferId = !string.IsNullOrWhiteSpace(targetSlot.WaferId);
                if (targetSlot.HasWafer != targetHasWaferId)
                    throw new InvalidOperationException("대상 cassette slot의 점유/Material ID가 불일치합니다. cassette=" + cassetteRole +
                                                        ", slot=" + (slotNumber + 1) +
                                                        ", hasWafer=" + targetSlot.HasWafer +
                                                        ", waferId=" + targetSlot.WaferId);

                if (targetSlot.HasWafer && !string.Equals(targetSlot.WaferId, waferId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("대상 cassette slot에 다른 자재가 있어 덮어쓸 수 없습니다. cassette=" + cassetteRole +
                                                        ", slot=" + (slotNumber + 1) +
                                                        ", targetWafer=" + targetSlot.WaferId +
                                                        ", movingWafer=" + waferId);

                WaferMaterial wafer = null;
                if (requestedWafer != null)
                {
                    string requestedInstanceId = EnsureWaferInstanceIdNoLock(requestedWafer);
                    wafer = State.Wafers.FirstOrDefault(w =>
                        w != null &&
                        string.Equals(
                            w.WaferInstanceId ?? "",
                            requestedInstanceId,
                            StringComparison.OrdinalIgnoreCase));
                    if (wafer == null)
                    {
                        throw new InvalidOperationException(
                            "Cassette로 이동할 물리 Material 세대를 State에서 찾을 수 없습니다. wafer=" +
                            waferId + ", instance=" + requestedInstanceId);
                    }
                }
                if (targetSlot.HasWafer)
                {
                    string slotReason;
                    WaferMaterial slotWafer = ResolveCassetteSlotWaferNoLock(targetSlot, out slotReason);
                    if (slotWafer == null)
                        throw new InvalidOperationException("대상 cassette slot Material pointer가 올바르지 않습니다. " + slotReason);
                    if (!string.Equals(slotWafer.WaferId, waferId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("대상 cassette slot의 물리 Material과 이동 요청 ID가 다릅니다. target=" +
                                                            slotWafer.WaferId + ", moving=" + waferId);
                    if (wafer != null && !IsSameWaferInstance(wafer, slotWafer))
                    {
                        throw new InvalidOperationException(
                            "대상 cassette slot에 같은 표시 ID의 다른 물리 Material 세대가 있습니다. wafer=" +
                            waferId +
                            ", movingInstance=" + EnsureWaferInstanceIdNoLock(wafer) +
                            ", slotInstance=" + EnsureWaferInstanceIdNoLock(slotWafer));
                    }
                    wafer = slotWafer;
                }
                if (wafer == null)
                {
                    List<WaferMaterial> activeCandidates = State.Wafers
                        .Where(w =>
                            w != null &&
                            string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase) &&
                            WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                        .ToList();
                    if (activeCandidates.Count > 1)
                    {
                        throw new InvalidOperationException(
                            "같은 표시 Wafer/Bin ID의 활성 물리 Material이 둘 이상이므로 문자열 ID만으로 Cassette에 넣을 수 없습니다. wafer=" +
                            waferId + ", candidates=" + activeCandidates.Count);
                    }
                    if (activeCandidates.Count == 1)
                        wafer = activeCandidates[0];
                }
                if (wafer == null)
                {
                    wafer = new WaferMaterial
                    {
                        WaferId = waferId,
                        CreatedAt = DateTime.Now
                    };
                    EnsureWaferInstanceIdNoLock(wafer);
                    State.Wafers.Add(wafer);
                }
                string resolvedLotId = ResolveOrCreateCassetteLotId(cassetteLotId, cassette, wafer);
                WaferMaterialState previousState = WaferMaterialStateText.Normalize(wafer.State);
                if (targetSlot.HasWafer && !IsWaferAtCassetteSlot(wafer, cassetteRole, slotNumber))
                {
                    throw new InvalidOperationException("대상 cassette slot과 이동 자재의 현재 위치가 중복/불일치 상태입니다. cassette=" + cassetteRole +
                                                        ", slot=" + (slotNumber + 1) +
                                                        ", wafer=" + wafer.WaferId +
                                                        ", currentLocation=" + wafer.CurrentLocation);
                }

                MaterialLocation targetLocation = MaterialLocation.Cassette(
                    IsOutputCassetteRole(cassetteRole) ? MaterialLocationKind.OutputCassette : MaterialLocationKind.InputCassette,
                    cassetteRole,
                    slotNumber);
                WaferMaterial otherAtTarget = FindOtherWaferAtLocation(wafer, targetLocation);
                if (otherAtTarget != null)
                {
                    throw new InvalidOperationException("대상 cassette slot에 다른 Material 위치 데이터가 있어 덮어쓸 수 없습니다. cassette=" + cassetteRole +
                                                        ", slot=" + (slotNumber + 1) +
                                                        ", targetWafer=" + otherAtTarget.WaferId +
                                                        ", movingWafer=" + wafer.WaferId);
                }

                if (wafer.SourceSlotNumber >= 0 &&
                    previousState != WaferMaterialState.Empty &&
                    (wafer.SourceCassetteRole != cassetteRole || wafer.SourceSlotNumber != slotNumber))
                {
                    throw new InvalidOperationException("자재를 원본 cassette/slot이 아닌 위치로 반환할 수 없습니다. wafer=" + wafer.WaferId +
                                                        ", source=" + wafer.SourceCassetteRole + "/" + (wafer.SourceSlotNumber + 1) +
                                                        ", target=" + cassetteRole + "/" + (slotNumber + 1));
                }

                MaterialLocation previousLocation = wafer.CurrentLocation;
                RemoveWaferFromCassetteSlot(wafer);

                if (wafer.SourceSlotNumber < 0 || previousState == WaferMaterialState.Empty)
                {
                    wafer.SourceCassetteId = cassette.CassetteId;
                    wafer.SourceCassetteRole = cassetteRole;
                    wafer.SourceSlotNumber = slotNumber;
                    if (double.IsNaN(wafer.SourceCassetteSlotPosition) && !double.IsNaN(slotPosition))
                        wafer.SourceCassetteSlotPosition = slotPosition;
                }

                wafer.CassetteLotId = resolvedLotId;
                wafer.CurrentLocation = MaterialLocation.Cassette(
                    cassetteRole == CassetteMaterialRole.Input1 || cassetteRole == CassetteMaterialRole.Input2
                        ? MaterialLocationKind.InputCassette
                        : MaterialLocationKind.OutputCassette,
                    cassetteRole,
                    slotNumber);
                if (IsOutputCassetteRole(cassetteRole))
                {
                    wafer.OutputCassetteId = cassette.CassetteId;
                    wafer.OutputCassetteRole = cassetteRole;
                    wafer.OutputSlotNumber = slotNumber;
                }
                if (!double.IsNaN(slotPosition))
                    wafer.CurrentCassetteSlotPosition = slotPosition;
                if (updateState)
                    wafer.State = WaferMaterialStateText.Normalize(state);
                wafer.UpdatedAt = DateTime.Now;

                cassette.CassetteLotId = resolvedLotId;
                targetSlot.WaferId = wafer.WaferId;
                targetSlot.WaferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
                targetSlot.HasWafer = true;
                cassette.LastScanTime = DateTime.Now;

                int synchronizedDieCount = SynchronizeCassetteReturnDieLocationsNoLock(
                    wafer,
                    cassetteRole,
                    slotNumber,
                    previousLocation);
                CloseResultFileSessionForCassetteReturnNoLock(
                    wafer,
                    cassetteRole,
                    previousLocation);

                SequenceTrace.MaterialChange(
                    "PutWaferInCassette",
                    "wafer=" + wafer.WaferId,
                    "from=" + previousLocation,
                    "to=" + wafer.CurrentLocation,
                    "state=" + wafer.State,
                    "slot=" + slotNumber,
                    "cassette=" + cassetteRole,
                    "dieLocations=" + synchronizedDieCount);
            }
            NotifyAndSave("PutWaferInCassette");
        }

        private static int SynchronizeCassetteReturnDieLocationsNoLock(
            WaferMaterial wafer,
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            MaterialLocation previousWaferLocation)
        {
            if (wafer == null ||
                previousWaferLocation == null ||
                State.Dies == null)
            {
                return 0;
            }

            bool isOutputCassette = IsOutputCassetteRole(cassetteRole);
            MaterialLocationKind feederLocation = isOutputCassette
                ? MaterialLocationKind.OutputFeeder
                : MaterialLocationKind.InputFeeder;
            if (previousWaferLocation.Kind != feederLocation)
                return 0;

            string waferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
            if (string.IsNullOrWhiteSpace(waferInstanceId))
                return 0;

            MaterialLocationKind cassetteLocation = isOutputCassette
                ? MaterialLocationKind.OutputCassette
                : MaterialLocationKind.InputCassette;
            MaterialLocationKind outputStageLocation =
                cassetteRole == CassetteMaterialRole.Ng1
                    ? MaterialLocationKind.OutputStageNg
                    : MaterialLocationKind.OutputStageGood;
            DateTime updatedAt = DateTime.Now;
            int synchronizedCount = 0;

            foreach (DieMaterial die in State.Dies)
            {
                if (die == null || die.CurrentLocation == null)
                    continue;

                MaterialLocationKind dieLocation = die.CurrentLocation.Kind;
                bool shouldSynchronize;
                if (isOutputCassette)
                {
                    shouldSynchronize =
                        !string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) &&
                        string.Equals(
                            die.OutputWaferInstanceId,
                            waferInstanceId,
                            StringComparison.OrdinalIgnoreCase) &&
                        (dieLocation == outputStageLocation ||
                         dieLocation == MaterialLocationKind.OutputFeeder);
                }
                else
                {
                    shouldSynchronize =
                        !string.IsNullOrWhiteSpace(die.InputWaferInstanceId) &&
                        string.Equals(
                            die.InputWaferInstanceId,
                            waferInstanceId,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) &&
                        (dieLocation == MaterialLocationKind.InputStage ||
                         dieLocation == MaterialLocationKind.InputFeeder);
                }

                if (!shouldSynchronize)
                    continue;

                die.CurrentLocation = MaterialLocation.Cassette(
                    cassetteLocation,
                    cassetteRole,
                    slotNumber);
                die.UpdatedAt = updatedAt;
                synchronizedCount++;
            }

            return synchronizedCount;
        }

        public static WaferMaterial GetOrCreateWaferInMappedCassette(
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string cassetteLotId,
            double slotPosition = double.NaN)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null || !cassette.IsMapped)
                return null;

            cassette.EnsureSlots();
            if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                return null;

            var slot = cassette.Slots[slotNumber];
            string waferId = string.IsNullOrEmpty(slot.WaferId)
                ? BuildGeneratedWaferId(cassetteRole, slotNumber)
                : slot.WaferId;

            string slotReason;
            var wafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
            if (wafer == null &&
                (!string.IsNullOrWhiteSpace(slot.WaferInstanceId) ||
                 (!string.IsNullOrWhiteSpace(slot.WaferId) &&
                  State.Wafers.Count(w => w != null &&
                      string.Equals(w.WaferId, slot.WaferId, StringComparison.OrdinalIgnoreCase)) > 1)))
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Mapped cassette slot Material resolve failed. cassette=" + cassetteRole +
                    ", slot=" + (slotNumber + 1) +
                    ", reason=" + slotReason + " - Blocked");
                return null;
            }
            if (wafer == null)
            {
                wafer = new WaferMaterial
                {
                    WaferId = waferId,
                    CreatedAt = DateTime.Now
                };
                State.Wafers.Add(wafer);
            }

            ApplyWaferCassetteLocation(wafer, cassette, slotNumber, cassetteLotId, slotPosition);
            if (string.IsNullOrWhiteSpace(wafer.TapeFrameSpecName))
                wafer.TapeFrameSpecName = ResolveCassetteTapeFrameSpecName(cassette);
            slot.WaferId = wafer.WaferId;
            slot.WaferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
            slot.HasWafer = true;
            NotifyAndSave("CreateWaferInMappedCassette");
            return wafer;
        }

        public static WaferMaterial GetWaferInCassette(CassetteMaterialRole cassetteRole, int slotNumber)
        {
            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null || !cassette.IsMapped)
                return null;

            cassette.EnsureSlots();
            if (slotNumber < 0 || slotNumber >= cassette.Slots.Count)
                return null;

            var slot = cassette.Slots[slotNumber];
            if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                return null;

            string reason;
            WaferMaterial wafer = ResolveCassetteSlotWaferNoLock(slot, out reason);
            if (wafer == null)
            {
                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Cassette slot Material pointer mismatch. cassette=" + cassetteRole +
                    ", slot=" + (slotNumber + 1) +
                    ", reason=" + reason + " - Blocked");
            }
            return wafer;
        }

        public static WaferMaterial GetWaferAtLocation(MaterialLocationKind kind)
        {
            // [계약 보강 2026-08-07] UI 등 락 밖 호출자가 변이 중인 Wafers를 순회하지 않도록 락을 잡는다.
            // 시퀀스 경로는 이미 _stateSync를 보유한 채 호출하므로 재진입이라 추가 비용이 거의 없다.
            lock (_stateSync)
            {
                return State.Wafers.FirstOrDefault(w =>
                    w.CurrentLocation != null &&
                    w.CurrentLocation.Kind == kind &&
                    WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty);
            }
        }

        /// <summary>
        /// START/재개 전에 InputStage 또는 InputFeeder에 남아 있는 활성 wafer의 원본 Input cassette/slot
        /// Material이 안전하게 식별 가능한지 확인한다. 원본 slot은 wafer가 장비 안으로 이동한 동안 비어 있는
        /// 것이 정상이므로 Empty는 허용하고, 미매핑/범위 오류/다른 wafer 점유만 실패로 처리한다.
        /// </summary>
        public static bool TryValidateActiveInputWaferSourceState(out string reason)
        {
            lock (_stateSync)
            {
                reason = "";
                if (State == null || State.Wafers == null || State.Cassettes == null)
                {
                    reason = "Material snapshot이 초기화되지 않았습니다.";
                    return false;
                }

                List<WaferMaterial> activeWafers = State.Wafers
                    .Where(w =>
                        w != null &&
                        w.CurrentLocation != null &&
                        (w.CurrentLocation.Kind == MaterialLocationKind.InputStage ||
                         w.CurrentLocation.Kind == MaterialLocationKind.InputFeeder) &&
                        WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                    .ToList();

                foreach (WaferMaterial wafer in activeWafers)
                {
                    if (!TryValidateActiveInputWaferSourceStateNoLock(wafer, out reason))
                        return false;
                }

                return true;
            }
        }

        public static bool TryValidateActiveInputWaferSourceState(WaferMaterial wafer, out string reason)
        {
            lock (_stateSync)
            {
                return TryValidateActiveInputWaferSourceStateNoLock(wafer, out reason);
            }
        }

        private static bool TryValidateActiveInputWaferSourceStateNoLock(
            WaferMaterial wafer,
            out string reason)
        {
            reason = "";
            if (State == null || State.Wafers == null || State.Cassettes == null)
            {
                reason = "Material snapshot이 초기화되지 않았습니다.";
                return false;
            }

            if (wafer == null)
            {
                reason = "활성 Input wafer Material이 null입니다.";
                return false;
            }

            string waferId = string.IsNullOrWhiteSpace(wafer.WaferId) ? "(없음)" : wafer.WaferId.Trim();
            MaterialLocationKind location = wafer.CurrentLocation != null
                ? wafer.CurrentLocation.Kind
                : MaterialLocationKind.Unknown;
            if (location != MaterialLocationKind.InputStage &&
                location != MaterialLocationKind.InputFeeder)
            {
                reason = "활성 Input wafer 위치가 Stage/Feeder가 아닙니다. wafer=" + waferId +
                         ", location=" + location;
                return false;
            }

            if (WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
            {
                reason = "활성 Input wafer 상태가 Empty입니다. wafer=" + waferId +
                         ", location=" + location;
                return false;
            }

            if (string.IsNullOrWhiteSpace(wafer.WaferId))
            {
                reason = "활성 Input wafer ID가 없습니다. location=" + location;
                return false;
            }

            if (wafer.SourceCassetteRole != CassetteMaterialRole.Input1 &&
                wafer.SourceCassetteRole != CassetteMaterialRole.Input2)
            {
                reason = "활성 Input wafer의 원본 Cassette 역할이 유효하지 않습니다. wafer=" + waferId +
                         ", location=" + location +
                         ", sourceRole=" + wafer.SourceCassetteRole;
                return false;
            }

            if (wafer.SourceSlotNumber < 0)
            {
                reason = "활성 Input wafer의 원본 Slot 정보가 없습니다. wafer=" + waferId +
                         ", location=" + location +
                         ", sourceRole=" + wafer.SourceCassetteRole +
                         ", sourceSlot=" + wafer.SourceSlotNumber;
                return false;
            }

            CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c =>
                c != null && c.Role == wafer.SourceCassetteRole);
            if (cassette == null)
            {
                reason = "활성 Input wafer의 원본 Cassette Material이 없습니다. wafer=" + waferId +
                         ", location=" + location +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00");
                return false;
            }

            if (!cassette.IsEnabled || !cassette.IsPresent || !cassette.IsMapped)
            {
                reason = "활성 Input wafer의 원본 Cassette가 운전 가능한 상태가 아닙니다. wafer=" + waferId +
                         ", location=" + location +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00") +
                         ", enabled=" + cassette.IsEnabled +
                         ", present=" + cassette.IsPresent +
                         ", mapped=" + cassette.IsMapped;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(wafer.SourceCassetteId) &&
                !string.IsNullOrWhiteSpace(cassette.CassetteId) &&
                !string.Equals(
                    wafer.SourceCassetteId.Trim(),
                    cassette.CassetteId.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                reason = "활성 Input wafer의 원본 Cassette ID가 현재 Cassette와 다릅니다. wafer=" + waferId +
                         ", location=" + location +
                         ", sourceCassetteId=" + wafer.SourceCassetteId +
                         ", currentCassetteId=" + cassette.CassetteId +
                         ", sourceRole=" + wafer.SourceCassetteRole;
                return false;
            }

            if (cassette.SlotCount <= 0 ||
                wafer.SourceSlotNumber >= cassette.SlotCount ||
                cassette.Slots == null ||
                wafer.SourceSlotNumber >= cassette.Slots.Count)
            {
                reason = "활성 Input wafer의 원본 Slot이 Cassette 범위를 벗어났습니다. wafer=" + waferId +
                         ", location=" + location +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00") +
                         ", slotCount=" + cassette.SlotCount +
                         ", materialSlotCount=" + (cassette.Slots != null ? cassette.Slots.Count : 0);
                return false;
            }

            CassetteSlotMaterial sourceSlot = cassette.Slots[wafer.SourceSlotNumber];
            if (sourceSlot == null)
            {
                reason = "활성 Input wafer의 원본 Slot Material이 null입니다. wafer=" + waferId +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00");
                return false;
            }

            if (sourceSlot.HasWafer || !string.IsNullOrWhiteSpace(sourceSlot.WaferId))
            {
                reason = "활성 Input wafer의 원본 Slot에 다른 Cassette wafer 정보가 남아 있습니다. wafer=" +
                         waferId +
                         ", location=" + location +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00") +
                         ", slotHasWafer=" + sourceSlot.HasWafer +
                         ", slotWaferId=" + (sourceSlot.WaferId ?? "");
                return false;
            }

            WaferMaterial conflictingWafer = State.Wafers.FirstOrDefault(w =>
                w != null &&
                !ReferenceEquals(w, wafer) &&
                w.CurrentLocation != null &&
                w.CurrentLocation.Kind == MaterialLocationKind.InputCassette &&
                w.CurrentLocation.CassetteRole == wafer.SourceCassetteRole &&
                w.CurrentLocation.SlotNumber == wafer.SourceSlotNumber &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty);
            if (conflictingWafer != null)
            {
                reason = "활성 Input wafer의 원본 Slot을 다른 wafer 위치 정보가 점유하고 있습니다. wafer=" +
                         waferId +
                         ", conflictingWafer=" + (conflictingWafer.WaferId ?? "") +
                         ", source=" + wafer.SourceCassetteRole + "/S" +
                         (wafer.SourceSlotNumber + 1).ToString("00");
                return false;
            }

            return true;
        }

        public static WaferMaterial CreateWaferAtLocation(MaterialLocationKind kind, string waferId, WaferMaterialState state)
        {
            var wafer = GetOrCreateWafer(waferId);
            RemoveWaferFromCassetteSlot(wafer);
            wafer.CurrentLocation = new MaterialLocation { Kind = kind };
            wafer.State = WaferMaterialStateText.Normalize(state);
            if (string.IsNullOrWhiteSpace(wafer.TapeFrameSpecName))
                wafer.TapeFrameSpecName = kind == MaterialLocationKind.InputStage
                    ? ResolveInputTapeFrameSpecName(0)
                    : ResolveRecipeTapeFrameSpecName(0);
            wafer.UpdatedAt = DateTime.Now;
            NotifyAndSave("CreateWaferAtLocation");
            return wafer;
        }

    }
}
