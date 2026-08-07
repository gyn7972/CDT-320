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
    // MaterialStateService partial: Wafer 이동, 바코드, DataOnly 이동/삭제 (원본 4450-5797)
    public static partial class MaterialStateService
    {
        public static void MoveWaferToInputFeeder(WaferMaterial wafer)
        {
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
                return;

            MoveWafer(
                wafer,
                new MaterialLocation { Kind = MaterialLocationKind.InputFeeder },
                WaferMaterialState.WorkReady);
        }

        public static void MoveWaferToInputStage(WaferMaterial wafer)
        {
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId))
                return;

            MoveWafer(
                wafer,
                new MaterialLocation { Kind = MaterialLocationKind.InputStage },
                WaferMaterialState.Working);
        }

        public static bool UpdateWaferFieldInMappedCassette(
            CassetteMaterialRole cassetteRole,
            int slotNumber,
            string fieldKey,
            string value)
        {
            var wafer = GetOrCreateWaferInMappedCassette(cassetteRole, slotNumber, "");
            if (wafer == null || string.IsNullOrEmpty(fieldKey))
                return false;

            var cassette = State.Cassettes.FirstOrDefault(c => c.Role == cassetteRole);
            if (cassette == null)
                return false;

            string newValue = value ?? "";
            if (fieldKey == "WaferId")
            {
                if (string.IsNullOrWhiteSpace(newValue))
                    return false;

                wafer.WaferId = newValue;
                cassette.Slots[slotNumber].WaferId = newValue;
            }
            else if (fieldKey == "CassetteLotId")
            {
                wafer.CassetteLotId = newValue;
                cassette.CassetteLotId = newValue;
            }
            else if (fieldKey == "TapeFrameSpecName")
            {
                wafer.TapeFrameSpecName = newValue;
            }
            else if (fieldKey == "State")
            {
                WaferMaterialState parsed;
                if (!WaferMaterialStateText.TryParse(newValue, out parsed))
                    return false;
                wafer.State = WaferMaterialStateText.Normalize(parsed);
                cassette.Slots[slotNumber].HasWafer = wafer.State != WaferMaterialState.Empty;
            }
            else
            {
                return false;
            }

            ApplyWaferCassetteLocation(wafer, cassette, slotNumber, wafer.CassetteLotId);
            if (fieldKey == "State")
            {
                WaferMaterialState parsed;
                if (WaferMaterialStateText.TryParse(newValue, out parsed))
                {
                    wafer.State = WaferMaterialStateText.Normalize(parsed);
                    cassette.Slots[slotNumber].HasWafer = wafer.State != WaferMaterialState.Empty;
                }
            }
            NotifyAndSave("UpdateWaferField");
            return true;
        }

        // To do: [Bin 상태 편집] 자재가 스테이지/피더에 나가 있어도 상태만 안전하게 바꾼다.
        //        기존 조건: 상태 변경도 UpdateWaferFieldInMappedCassette를 탔고, 그 안의
        //                   GetOrCreateWaferInMappedCassette + ApplyWaferCassetteLocation이
        //                   CurrentLocation을 카세트 슬롯으로 덮어써서, 스테이지에 나가 있는 자재를
        //                   슬롯으로 끌어오는 부작용이 있었다(슬롯 포인터까지 다시 채움).
        //        현재 기준: 지정 Material의 State만 바꾸고 위치/슬롯 포인터는 옮기지 않는다.
        //                   그 Material을 이미 가리키는 슬롯이 있으면 점유 플래그만 State에 맞춰 정리한다.
        /// <summary>
        /// Bin/Wafer 상태만 변경합니다(위치 이동 없음). 성공 시 저장을 요청하며,
        /// 수동 UI처럼 저장 완료 확인이 필요한 호출자는 TryFlushPendingSave를 이어서 호출합니다.
        /// </summary>
        public static bool UpdateWaferStateOnly(string waferId, string stateText, string userName)
        {
            if (string.IsNullOrWhiteSpace(waferId))
                return false;

            WaferMaterialState parsed;
            if (!WaferMaterialStateText.TryParse(stateText, out parsed))
            {
                Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "UpdateWaferStateOnly",
                        "Material 상태 변경 실패: 상태 문자열을 해석할 수 없습니다. material=" + waferId +
                    ", state=" + (stateText ?? "") + " - Failed");
                return false;
            }

            WaferMaterialState normalized = WaferMaterialStateText.Normalize(parsed);
            WaferMaterialState before;
            string locationText;

            lock (_stateSync)
            {
                List<WaferMaterial> candidates = State.Wafers != null
                    ? State.Wafers.Where(w => w != null &&
                        string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase)).ToList()
                    : new List<WaferMaterial>();
                if (candidates.Count != 1)
                {
                    Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "UpdateWaferStateOnly",
                        "Material 상태 변경 실패: 표시 ID로 물리 Material을 1개로 확정할 수 없습니다. material=" +
                        waferId + ", candidates=" + candidates.Count + " - Failed");
                    return false;
                }

                WaferMaterial wafer = candidates[0];
                string waferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
                before = WaferMaterialStateText.Normalize(wafer.State);
                wafer.State = normalized;
                wafer.UpdatedAt = DateTime.Now;
                locationText = wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";

                // 위치는 그대로 두고, 이 Material을 가리키고 있는 슬롯의 점유 플래그만 정리한다.
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

                            bool sameInstance = !string.IsNullOrWhiteSpace(slot.WaferInstanceId)
                                ? string.Equals(
                                    slot.WaferInstanceId,
                                    waferInstanceId,
                                    StringComparison.OrdinalIgnoreCase)
                                : string.Equals(
                                    slot.WaferId,
                                    wafer.WaferId,
                                    StringComparison.OrdinalIgnoreCase);
                            if (!sameInstance)
                                continue;

                            slot.WaferInstanceId = waferInstanceId;
                            slot.HasWafer = normalized != WaferMaterialState.Empty;
                        }
                    }
                }
            }

            Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "UpdateWaferStateOnly",
                "Material 상태 변경 완료(위치 이동 없음). material=" + waferId +
                ", before=" + WaferMaterialStateText.ToDisplayName(before) +
                ", after=" + WaferMaterialStateText.ToDisplayName(normalized) +
                ", location=" + locationText + " - Ok");

            NotifyAndSave("UpdateWaferStateOnly");
            return true;
        }

        /// <summary>
        /// Stage에 있는 물리 Wafer/Bin의 임시 ID를 실제 바코드로 원자 승격합니다.
        /// 물리 식별에는 WaferInstanceId를 계속 사용하며, 연관 Die/수신 계획/slot 포인터의 표시 ID도
        /// 같은 lock 안에서 함께 갱신합니다.
        /// </summary>
        public static bool TryApplyWaferBarcode(
            string waferInstanceId,
            MaterialLocationKind expectedLocation,
            string barcode,
            string source,
            int attempts,
            out string previousWaferId,
            out string reason)
        {
            previousWaferId = "";
            reason = "";

            string normalizedInstanceId = (waferInstanceId ?? "").Trim();
            string normalizedBarcode = NormalizeBarcodeValue(barcode);
            if (string.IsNullOrWhiteSpace(normalizedInstanceId))
            {
                reason = "바코드를 적용할 WaferInstanceId가 없습니다.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(normalizedBarcode) ||
                string.Equals(normalizedBarcode, "WAFER-NULL-ID", StringComparison.OrdinalIgnoreCase))
            {
                reason = "유효한 바코드 값이 없습니다.";
                return false;
            }
            if (expectedLocation == MaterialLocationKind.Unknown)
            {
                reason = "바코드 적용 대상 Stage 위치가 지정되지 않았습니다.";
                return false;
            }

            string locationText;
            DateTime updatedAt = DateTime.Now;
            lock (_stateSync)
            {
                List<WaferMaterial> candidates = State != null && State.Wafers != null
                    ? State.Wafers.Where(w =>
                        w != null &&
                        string.Equals(
                            w.WaferInstanceId ?? "",
                            normalizedInstanceId,
                            StringComparison.OrdinalIgnoreCase)).ToList()
                    : new List<WaferMaterial>();
                if (candidates.Count != 1)
                {
                    reason = "WaferInstanceId로 물리 Material을 1개로 확정할 수 없습니다. instance=" +
                             normalizedInstanceId + ", candidates=" + candidates.Count;
                    return false;
                }

                WaferMaterial wafer = candidates[0];
                MaterialLocation location = wafer.CurrentLocation;
                if (location == null || location.Kind != expectedLocation)
                {
                    reason = "바코드 대상 Material 위치가 변경되었습니다. expected=" + expectedLocation +
                             ", actual=" + (location != null ? location.Kind.ToString() : "null") +
                             ", wafer=" + (wafer.WaferId ?? "") +
                             ", instance=" + normalizedInstanceId;
                    return false;
                }
                if (WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
                {
                    reason = "빈 Material에는 바코드를 적용할 수 없습니다. location=" + expectedLocation +
                             ", instance=" + normalizedInstanceId;
                    return false;
                }

                WaferMaterial duplicate = State.Wafers.FirstOrDefault(w =>
                    w != null &&
                    !string.Equals(
                        w.WaferInstanceId ?? "",
                        normalizedInstanceId,
                        StringComparison.OrdinalIgnoreCase) &&
                    WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty &&
                    (string.Equals(w.WaferId ?? "", normalizedBarcode, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(w.BarcodeId ?? "", normalizedBarcode, StringComparison.OrdinalIgnoreCase)));
                if (duplicate != null)
                {
                    reason = "같은 바코드를 사용하는 다른 활성 Material이 있습니다. barcode=" +
                             normalizedBarcode + ", otherWafer=" + (duplicate.WaferId ?? "") +
                             ", otherInstance=" + (duplicate.WaferInstanceId ?? "") +
                             ", otherLocation=" +
                             (duplicate.CurrentLocation != null ? duplicate.CurrentLocation.ToString() : "null");
                    return false;
                }

                previousWaferId = wafer.WaferId ?? "";
                if (string.IsNullOrWhiteSpace(wafer.OriginalWaferId))
                    wafer.OriginalWaferId = previousWaferId;
                wafer.WaferId = normalizedBarcode;
                wafer.BarcodeId = normalizedBarcode;
                wafer.BarcodeConfirmed = true;
                wafer.BarcodeSource = string.IsNullOrWhiteSpace(source) ? "BARCODE" : source.Trim();
                wafer.BarcodeUpdatedAt = updatedAt;
                wafer.BarcodeAttemptCount = Math.Max(1, attempts);
                wafer.UpdatedAt = updatedAt;
                locationText = location.ToString();

                if (State.Dies != null)
                {
                    foreach (DieMaterial die in State.Dies)
                    {
                        if (die == null)
                            continue;

                        bool changed = false;
                        if (string.Equals(
                            die.InputWaferInstanceId ?? "",
                            normalizedInstanceId,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            die.WaferID_Input = normalizedBarcode;
                            changed = true;
                        }
                        if (string.Equals(
                            die.OutputWaferInstanceId ?? "",
                            normalizedInstanceId,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            die.WaferID_Output = normalizedBarcode;
                            changed = true;
                        }
                        if (changed)
                            die.UpdatedAt = updatedAt;
                    }
                }

                foreach (WaferMaterial linkedWafer in State.Wafers)
                {
                    if (linkedWafer == null ||
                        !string.Equals(
                            linkedWafer.OutputReceiveSourceWaferInstanceId ?? "",
                            normalizedInstanceId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    linkedWafer.OutputReceiveSourceWaferId = normalizedBarcode;
                    linkedWafer.UpdatedAt = updatedAt;
                }

                if (State.Cassettes != null)
                {
                    foreach (CassetteMaterial cassette in State.Cassettes)
                    {
                        if (cassette == null || cassette.Slots == null)
                            continue;
                        foreach (CassetteSlotMaterial slot in cassette.Slots)
                        {
                            if (slot != null &&
                                string.Equals(
                                    slot.WaferInstanceId ?? "",
                                    normalizedInstanceId,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                slot.WaferId = normalizedBarcode;
                            }
                        }
                    }
                }

                DieTapeFrame frame = !string.IsNullOrWhiteSpace(wafer.DieMapFrameObjId)
                    ? MaterialStorage.GetFrame(wafer.DieMapFrameObjId)
                    : null;
                if (frame == null && !string.IsNullOrWhiteSpace(previousWaferId))
                    frame = MaterialStorage.GetFrame(previousWaferId);
                if (frame != null)
                    frame.BarcodeId = normalizedBarcode;
            }

            NotifyAndSave("ApplyWaferBarcode");
            Log.Write(
                "Main",
                string.IsNullOrWhiteSpace(source) ? "SYSTEM" : source,
                "ApplyWaferBarcode",
                "Material 바코드 적용 완료. previous=" + previousWaferId +
                ", barcode=" + normalizedBarcode +
                ", instance=" + normalizedInstanceId +
                ", location=" + locationText +
                ", attempts=" + Math.Max(1, attempts) + " - Ok");
            return true;
        }

        private static string NormalizeBarcodeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var chars = new List<char>(value.Length);
            foreach (char ch in value)
            {
                if (ch == '\x02' || ch == '\x03' || char.IsWhiteSpace(ch))
                    continue;
                if (char.IsControl(ch))
                    return "";
                chars.Add(ch);
            }
            return new string(chars.ToArray());
        }

        public static void MoveWafer(string waferId, MaterialLocation location, WaferMaterialState state)
        {
            lock (_stateSync)
            {
                if (string.IsNullOrWhiteSpace(waferId))
                    throw new InvalidOperationException("이동할 Wafer/Bin ID가 없습니다.");

                List<WaferMaterial> candidates = State.Wafers
                    .Where(w =>
                        w != null &&
                        string.Equals(w.WaferId, waferId, StringComparison.OrdinalIgnoreCase) &&
                        WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                    .ToList();
                if (candidates.Count > 1)
                {
                    throw new InvalidOperationException(
                        "같은 표시 Wafer/Bin ID의 활성 물리 Material이 둘 이상이므로 문자열 ID만으로 이동할 수 없습니다. wafer=" +
                        waferId + ", candidates=" + candidates.Count);
                }

                WaferMaterial wafer = candidates.Count == 1 ? candidates[0] : GetOrCreateWafer(waferId);
                MoveWaferNoLock(wafer, location, state);
            }
            NotifyAndSave("MoveWafer");
        }

        public static void MoveWafer(WaferMaterial wafer, MaterialLocation location, WaferMaterialState state)
        {
            lock (_stateSync)
            {
                if (wafer == null)
                    throw new InvalidOperationException("이동할 Wafer/Bin Material이 없습니다.");

                string instanceId = EnsureWaferInstanceIdNoLock(wafer);
                WaferMaterial stateWafer = State.Wafers.FirstOrDefault(w =>
                    w != null &&
                    string.Equals(
                        w.WaferInstanceId ?? "",
                        instanceId,
                        StringComparison.OrdinalIgnoreCase));
                if (stateWafer == null)
                {
                    throw new InvalidOperationException(
                        "이동할 물리 Material 세대를 State에서 찾을 수 없습니다. wafer=" +
                        wafer.WaferId + ", instance=" + instanceId);
                }

                MoveWaferNoLock(stateWafer, location, state);
            }
            NotifyAndSave("MoveWafer");
        }

        private static void MoveWaferNoLock(
            WaferMaterial wafer,
            MaterialLocation location,
            WaferMaterialState state)
        {
            MaterialLocation targetLocation = location ?? MaterialLocation.Unknown();
            WaferMaterial occupied = FindOtherWaferAtLocation(wafer, targetLocation);
            if (occupied != null)
            {
                throw new InvalidOperationException("대상 Material 위치에 다른 자재가 있어 이동할 수 없습니다. target=" + targetLocation +
                                                    ", targetWafer=" + occupied.WaferId +
                                                    ", movingWafer=" + wafer.WaferId);
            }

            MaterialLocation previousLocation = wafer.CurrentLocation;
            RemoveWaferFromCassetteSlot(wafer);
            wafer.CurrentLocation = targetLocation;
            wafer.State = WaferMaterialStateText.Normalize(state);
            wafer.UpdatedAt = DateTime.Now;
            SequenceTrace.MaterialChange(
                "MoveWafer",
                "wafer=" + wafer.WaferId,
                "instance=" + EnsureWaferInstanceIdNoLock(wafer),
                "from=" + previousLocation,
                "to=" + wafer.CurrentLocation,
                "state=" + wafer.State);
        }

        // ===== DATA ONLY 수동 위치 이동/삭제 =====
        // 장비를 움직이지 않고 Material 위치 데이터만 변경한다. Motion/Cylinder/Vacuum/IO를 호출하지 않는다.
        // Source clear + Destination set + CurrentLocation 갱신을 하나의 lock에서 처리하고 즉시 저장한다.
        // Material ID/검사결과/DieMap은 유지한다.
        // State와 Cassette 위치 메타데이터는 실제 Auto 이송 규칙과 동일하게 목적지 기준으로 정규화한다.
        // 위치만 바꾸고 State를 유지하면 예: OutputCassette + Working 같은 불가능한 조합이 저장되어
        // 다음 Auto 시작 시 Material 일관성 검사에서 차단되므로 DATA ONLY에서도 반드시 함께 맞춘다.

        public static DataOnlyOperationResult MoveMaterialDataOnly(
            DataOnlyLocation source,
            DataOnlyLocation destination,
            string expectedMaterialId,
            string userName)
        {
            const string operation = "Move";
            try
            {
                string pathCode;
                string pathMessage;
                if (!ManualMaterialPositionService.ValidateAllowedPath(source, destination, out pathCode, out pathMessage))
                    return FailDataOnly(operation, source, destination, pathCode, pathMessage, userName);

                var result = new DataOnlyOperationResult
                {
                    Operation = operation,
                    SourceText = source.DisplayText,
                    DestinationText = destination.DisplayText
                };
                WaferMaterialState beforeState = WaferMaterialState.Empty;
                WaferMaterialState afterState = WaferMaterialState.Empty;
                WaferMaterialState displacedBeforeState = WaferMaterialState.Empty;
                WaferMaterialState displacedAfterState = WaferMaterialState.Empty;
                MaterialCompactionResult compactionResult = null;

                lock (_stateSync)
                {
                    // Apply 직전 재확인 1: Source Material 단일 존재/포인터 정합.
                    WaferMaterial wafer;
                    string failureCode;
                    string failureMessage;
                    if (!TryFindSingleWaferAtDataOnlyLocation(source, out wafer, out failureCode, out failureMessage))
                        return FailDataOnly(operation, source, destination, failureCode, failureMessage, userName);

                    // Preview 이후 Source Material이 바뀌었으면 적용하지 않는다.
                    if (!string.IsNullOrWhiteSpace(expectedMaterialId) &&
                        !string.Equals(wafer.WaferId, expectedMaterialId.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return FailDataOnly(operation, source, destination, "DATA-ONLY-SOURCE-CHANGED",
                            "선택(Preview) 이후 Source Material이 변경되었습니다. 다시 선택하십시오. expected=" +
                            expectedMaterialId + ", current=" + wafer.WaferId, userName);
                    }

                    // 기존 조건: "Apply 직전 재확인 2: Destination Empty."
                    // 현재 기준: Destination은 카세트 데이터 존재/슬롯 범위만 확인한다(점유는 교환으로 처리).
                    CassetteMaterial destinationCassette = null;
                    CassetteSlotMaterial destinationSlot = null;
                    if (destination.IsCassette)
                    {
                        destinationCassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == destination.CassetteRole);
                        if (destinationCassette == null)
                        {
                            return FailDataOnly(operation, source, destination, "DATA-ONLY-DEST-CASSETTE",
                                "Destination 카세트 상태 데이터가 없습니다. role=" + destination.CassetteRole, userName);
                        }

                        // 기존 조건: Input cassette destination은 enabled/present/mapped가 모두 참이어야 이동을 허용했다.
                        //            → 실물과 데이터가 어긋난 상태를 맞추려는데 카세트 상태 때문에 이동이 막혔다.
                        // 현재 기준: DATA ONLY는 유저가 장비 실물에 데이터를 맞추는 도구이므로 카세트 활성 상태로 막지 않고,
                        //            판정 근거만 로그로 남긴다(슬롯 범위/카세트 데이터 존재는 계속 검사한다).
                        // To do: [DATA ONLY 배선] 카세트 활성 상태 게이트 제거 - 상태 정렬 도구 목적 우선.
                        if (!destinationCassette.IsEnabled ||
                            !destinationCassette.IsPresent ||
                            !destinationCassette.IsMapped)
                        {
                            Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "DataOnlyMaterial",
                                "[DATA ONLY] Destination 카세트가 비활성 상태이지만 상태 정렬 목적으로 이동을 허용합니다. role=" +
                                destination.CassetteRole +
                                ", enabled=" + destinationCassette.IsEnabled +
                                ", present=" + destinationCassette.IsPresent +
                                ", mapped=" + destinationCassette.IsMapped + " - Check");
                        }

                        destinationCassette.EnsureSlots();
                        if (destination.SlotIndex < 0 || destination.SlotIndex >= destinationCassette.Slots.Count)
                        {
                            return FailDataOnly(operation, source, destination, "DATA-ONLY-DEST-SLOT-RANGE",
                                "Destination Slot이 카세트 범위를 벗어났습니다. role=" + destination.CassetteRole +
                                ", slot=" + (destination.SlotIndex + 1) + ", slotCount=" + destinationCassette.Slots.Count, userName);
                        }

                        destinationSlot = destinationCassette.Slots[destination.SlotIndex];
                        if (destinationSlot == null)
                        {
                            return FailDataOnly(operation, source, destination, "DATA-ONLY-DEST-SLOT",
                                "Destination Slot 데이터가 없습니다. " + destination.DisplayText, userName);
                        }

                    }

                    // 기존 조건: Destination이 점유되어 있으면 DATA-ONLY-DEST-OCCUPIED로 이동을 거부했다.
                    //            (카세트 슬롯 점유 검사 + FindOtherWaferAtLocation 검사 2곳)
                    //            → 실물은 그 슬롯에 있는데 데이터가 다른 자재로 채워져 있으면 정렬이 불가능했다.
                    // 현재 기준: 점유 자재를 Source 위치로 교환(swap)한다. 어느 Material 데이터도 삭제하지 않는다.
                    // To do: [DATA ONLY 교환] 점유 Destination은 거부 대신 Source 위치와 교환한다.
                    WaferMaterial displaced = null;
                    WaferMaterial destinationEmptyMarker = null;
                    List<DieMaterial> destinationMarkerOutputDies = null;
                    if (destination.IsCassette &&
                        (destinationSlot.HasWafer ||
                         !string.IsNullOrWhiteSpace(destinationSlot.WaferId) ||
                         !string.IsNullOrWhiteSpace(destinationSlot.WaferInstanceId)))
                    {
                        string destinationSlotReason;
                        WaferMaterial destinationMaterial = ResolveCassetteSlotWaferNoLock(
                            destinationSlot,
                            out destinationSlotReason);
                        if (destinationMaterial == null)
                        {
                            return FailDataOnly(
                                operation,
                                source,
                                destination,
                                "DATA-ONLY-DEST-POINTER",
                                "Destination Slot Material pointer가 올바르지 않습니다. " +
                                destinationSlotReason,
                                userName);
                        }

                        if (!destinationSlot.HasWafer)
                        {
                            if (!IsStateOnlyEmptyCassetteMaterialNoLock(
                                destinationMaterial,
                                destination.CassetteRole,
                                destination.SlotIndex))
                            {
                                return FailDataOnly(
                                    operation,
                                    source,
                                    destination,
                                    "DATA-ONLY-DEST-EMPTY-POINTER",
                                    "Destination의 비점유 pointer가 합법적인 EMPTY Material이 아닙니다.",
                                    userName);
                            }

                            destinationEmptyMarker = destinationMaterial;
                            if (IsOutputCassetteRole(destination.CassetteRole))
                            {
                                string markerClearReason;
                                if (!TryCollectOutputCassetteClearDiesNoLock(
                                    new List<WaferMaterial> { destinationEmptyMarker },
                                    out destinationMarkerOutputDies,
                                    out markerClearReason))
                                {
                                    return FailDataOnly(
                                        operation,
                                        source,
                                        destination,
                                        "DATA-ONLY-DEST-EMPTY-BLOCKED",
                                        markerClearReason,
                                        userName);
                                }
                            }
                        }
                        else
                        {
                            displaced = destinationMaterial;
                        }
                    }

                    if (displaced == null && destinationEmptyMarker == null)
                        displaced = FindOtherWaferAtLocation(wafer, destination.ToMaterialLocation());

                    List<DieMaterial> movingDies;
                    string dieMoveReason;
                    if (!TryCollectDataOnlyMoveDiesNoLock(wafer, source, out movingDies, out dieMoveReason))
                    {
                        return FailDataOnly(
                            operation,
                            source,
                            destination,
                            "DATA-ONLY-SOURCE-DIE-BLOCKED",
                            dieMoveReason,
                            userName);
                    }

                    List<DieMaterial> displacedDies = new List<DieMaterial>();
                    if (displaced != null &&
                        !TryCollectDataOnlyMoveDiesNoLock(
                            displaced,
                            destination,
                            out displacedDies,
                            out dieMoveReason))
                    {
                        return FailDataOnly(
                            operation,
                            source,
                            destination,
                            "DATA-ONLY-DEST-DIE-BLOCKED",
                            dieMoveReason,
                            userName);
                    }

                    // 원자 반영: 양쪽 pointer 제거 → Source 자재를 Destination에, 점유 자재를 Source 위치에 등록.
                    MaterialLocation beforeLocation = wafer.CurrentLocation;
                    MaterialLocation displacedBeforeLocation = displaced != null ? displaced.CurrentLocation : null;
                    beforeState = WaferMaterialStateText.Normalize(wafer.State);
                    displacedBeforeState = displaced != null
                        ? WaferMaterialStateText.Normalize(displaced.State)
                        : WaferMaterialState.Empty;
                    RemoveWaferFromCassetteSlot(wafer);
                    if (displaced != null)
                        RemoveWaferFromCassetteSlot(displaced);
                    if (destinationEmptyMarker != null)
                    {
                        RemoveWaferFromCassetteSlot(destinationEmptyMarker);
                        if (destinationMarkerOutputDies != null)
                        {
                            DetachOrRemoveClearedOutputDiesNoLock(destinationMarkerOutputDies);
                            ClearOutputStageWaferProcessingFieldsNoLock(destinationEmptyMarker);
                        }
                        destinationEmptyMarker.State = WaferMaterialState.Empty;
                        destinationEmptyMarker.CurrentLocation = MaterialLocation.Unknown();
                        destinationEmptyMarker.UpdatedAt = DateTime.Now;
                    }

                    ApplyDataOnlyPlacementNoLock(wafer, destination);
                    SynchronizeDataOnlyDieLocationsNoLock(movingDies, destination);
                    if (displaced != null)
                    {
                        ApplyDataOnlyPlacementNoLock(displaced, source);
                        SynchronizeDataOnlyDieLocationsNoLock(displacedDies, source);
                    }
                    afterState = WaferMaterialStateText.Normalize(wafer.State);
                    displacedAfterState = displaced != null
                        ? WaferMaterialStateText.Normalize(displaced.State)
                        : WaferMaterialState.Empty;

                    result.MaterialId = wafer.WaferId;
                    result.BeforeLocationText = beforeLocation != null ? beforeLocation.ToString() : "";
                    result.AfterLocationText = wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";
                    result.SwappedMaterialId = displaced != null ? displaced.WaferId : "";
                    result.SwappedToText = displaced != null ? source.DisplayText : "";

                    SequenceTrace.MaterialChange(
                        "DataOnlyMove",
                        "wafer=" + wafer.WaferId,
                        "from=" + beforeLocation,
                        "to=" + wafer.CurrentLocation,
                        "state=" + beforeState + "->" + afterState,
                        "user=" + (userName ?? ""),
                        "noMotion=true");

                    if (displaced != null)
                    {
                        SequenceTrace.MaterialChange(
                            "DataOnlyMoveSwap",
                            "wafer=" + displaced.WaferId,
                            "from=" + displacedBeforeLocation,
                            "to=" + displaced.CurrentLocation,
                            "state=" + displacedBeforeState + "->" + displacedAfterState,
                            "user=" + (userName ?? ""),
                            "noMotion=true");
                    }

                    compactionResult = CompactMaterialStateNoLock();
                }

                // 이동 결과를 즉시 Snapshot에 저장한다(백그라운드 스로틀 대기 없이 동기 flush).
                LogMaterialCompaction("DataOnlyManualMove", compactionResult);
                NotifyAndSave("DataOnlyManualMove");
                result.PersistenceSucceeded = TryFlushPendingSave("DataOnlyManualMove");
                if (!result.PersistenceSucceeded)
                {
                    result.Success = false;
                    result.FailureCode = "DATA-ONLY-MOVE-SAVE-FAIL";
                    result.FailureMessage =
                        "Material 위치는 메모리에서 변경되었지만 Snapshot 저장을 확인하지 못했습니다.";
                    Log.Write(
                        "Main",
                        string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName,
                        "DataOnlyMaterial",
                        "[DATA ONLY] Material 데이터 이동 후 저장 확인 실패(장비 무동작). material=" +
                        result.MaterialId + ", source=" + result.SourceText +
                        ", destination=" + result.DestinationText + " - Failed");
                    return result;
                }

                result.Success = true;

                Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "DataOnlyMaterial",
                    "[DATA ONLY] Material 데이터 이동 완료(장비 무동작). material=" + result.MaterialId +
                    ", source=" + result.SourceText +
                    ", destination=" + result.DestinationText +
                    ", state=" + beforeState + "->" + afterState +
                    ", swapped=" + (string.IsNullOrWhiteSpace(result.SwappedMaterialId) ? "-" : result.SwappedMaterialId) +
                    ", swappedTo=" + (string.IsNullOrWhiteSpace(result.SwappedToText) ? "-" : result.SwappedToText) +
                    ", swappedState=" + (string.IsNullOrWhiteSpace(result.SwappedMaterialId)
                        ? "-"
                        : displacedBeforeState + "->" + displacedAfterState) +
                    ", persisted=" + result.PersistenceSucceeded + " - Ok");
                return result;
            }
            catch (Exception ex)
            {
                return FailDataOnly(operation, source, destination, "DATA-ONLY-MOVE-EX",
                    "DATA ONLY 이동 중 예외가 발생했습니다: " + ex.Message, userName);
            }
            finally
            {
            }
        }

        public static DataOnlyOperationResult DeleteMaterialDataOnly(
            DataOnlyLocation location,
            string expectedMaterialId,
            string userName)
        {
            const string operation = "Delete";
            try
            {
                if (location == null)
                    return FailDataOnly(operation, null, null, "DATA-ONLY-DELETE-NULL", "삭제 위치가 지정되지 않았습니다.", userName);

                var result = new DataOnlyOperationResult
                {
                    Operation = operation,
                    SourceText = location.DisplayText,
                    DestinationText = ""
                };
                MaterialCompactionResult compactionResult;
                int removedDieCount;
                List<DieMaterial> relatedOutputDies = null;
                List<DieMaterial> relatedInputDies = null;
                List<DieMaterial> inputHistoryDies = null;
                bool preserveInputHistory = false;

                lock (_stateSync)
                {
                    WaferMaterial wafer;
                    string failureCode;
                    string failureMessage;
                    if (!TryFindSingleWaferAtDataOnlyLocation(location, out wafer, out failureCode, out failureMessage))
                        return FailDataOnly(operation, location, null, failureCode, failureMessage, userName);

                    if (!string.IsNullOrWhiteSpace(expectedMaterialId) &&
                        !string.Equals(wafer.WaferId, expectedMaterialId.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return FailDataOnly(operation, location, null, "DATA-ONLY-SOURCE-CHANGED",
                            "선택(Preview) 이후 대상 Material이 변경되었습니다. 다시 선택하십시오. expected=" +
                            expectedMaterialId + ", current=" + wafer.WaferId, userName);
                    }

                    MaterialLocation beforeLocation = wafer.CurrentLocation;
                    bool outputLocation = location.Kind == MaterialLocationKind.OutputStageGood ||
                                          location.Kind == MaterialLocationKind.OutputStageNg ||
                                          location.Kind == MaterialLocationKind.OutputFeeder ||
                                          location.Kind == MaterialLocationKind.OutputCassette;
                    if (outputLocation)
                    {
                        string outputClearReason;
                        bool canClearOutput = location.Kind == MaterialLocationKind.OutputCassette
                            ? TryCollectOutputCassetteClearDiesNoLock(
                                new List<WaferMaterial> { wafer },
                                out relatedOutputDies,
                                out outputClearReason)
                            : TryCollectOutputLocationClearDiesNoLock(
                                new List<WaferMaterial> { wafer },
                                location.Kind,
                                out relatedOutputDies,
                                out outputClearReason);
                        if (!canClearOutput)
                        {
                            return FailDataOnly(
                                operation,
                                location,
                                null,
                                "DATA-ONLY-OUTPUT-DIE-BLOCKED",
                                outputClearReason,
                                userName);
                        }
                    }
                    else if (location.Kind == MaterialLocationKind.InputStage ||
                             location.Kind == MaterialLocationKind.InputFeeder)
                    {
                        string inputClearReason;
                        if (!TryCollectInputLocationClearDiesNoLock(
                            new List<WaferMaterial> { wafer },
                            location.Kind,
                            out relatedInputDies,
                            out inputClearReason))
                        {
                            return FailDataOnly(
                                operation,
                                location,
                                null,
                                "DATA-ONLY-INPUT-DIE-BLOCKED",
                                inputClearReason,
                                userName);
                        }
                        if (!TryCollectInputHistoryForClearNoLock(
                            new List<WaferMaterial> { wafer },
                            out inputHistoryDies,
                            out preserveInputHistory,
                            out inputClearReason))
                        {
                            return FailDataOnly(
                                operation,
                                location,
                                null,
                                "DATA-ONLY-INPUT-HISTORY-BLOCKED",
                                inputClearReason,
                                userName);
                        }
                    }

                    if (outputLocation)
                    {
                        removedDieCount = DetachOrRemoveClearedOutputDiesNoLock(relatedOutputDies);
                    }
                    else if (relatedInputDies != null)
                    {
                        if (preserveInputHistory)
                        {
                            DemotePreservedInputHistoryLocationsNoLock(inputHistoryDies, location.Kind);
                            removedDieCount = 0;
                        }
                        else
                        {
                            removedDieCount = RemoveDieMaterialsNoLock(relatedInputDies);
                        }
                    }
                    else
                    {
                        removedDieCount = location.IsCassette
                            ? 0
                            : RemoveDiesAtClearedLocationNoLock(
                                new List<WaferMaterial> { wafer },
                                location.Kind);
                    }

                    if ((location.Kind == MaterialLocationKind.InputStage ||
                         location.Kind == MaterialLocationKind.InputFeeder) &&
                        !preserveInputHistory)
                    {
                        ClearInputStageWaferProcessingFieldsNoLock(wafer);
                        wafer.InputStageProcessingGeneration = wafer.InputStageProcessingGeneration + 1;
                        InputStageHybridResultSession.Clear();
                    }
                    else if (location.Kind == MaterialLocationKind.OutputStageGood ||
                             location.Kind == MaterialLocationKind.OutputStageNg ||
                             location.Kind == MaterialLocationKind.OutputFeeder ||
                             location.Kind == MaterialLocationKind.OutputCassette)
                    {
                        ClearOutputStageWaferProcessingFieldsNoLock(wafer);
                        if (location.Kind == MaterialLocationKind.OutputStageGood ||
                            location.Kind == MaterialLocationKind.OutputStageNg)
                        {
                            QMC.CDT320.BinSide side = location.Kind == MaterialLocationKind.OutputStageNg
                                ? QMC.CDT320.BinSide.Ng
                                : QMC.CDT320.BinSide.Good;
                            _outputReceiveOrderCache.Remove(side);
                        }
                    }

                    // 기존 중앙 삭제 계약(Clear*)과 동일: 위치 pointer 제거 + 논리 삭제(State=Empty, Location=Unknown).
                    // 잔존 CassetteLotId가 다음 mapping 등록을 막지 않도록 함께 비운다.
                    RemoveWaferFromCassetteSlot(wafer);
                    wafer.State = WaferMaterialState.Empty;
                    wafer.CurrentLocation = MaterialLocation.Unknown();
                    wafer.CassetteLotId = "";
                    wafer.UpdatedAt = DateTime.Now;

                    result.MaterialId = wafer.WaferId;
                    result.BeforeLocationText = beforeLocation != null ? beforeLocation.ToString() : "";
                    result.AfterLocationText = wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";

                    SequenceTrace.MaterialChange(
                        "DataOnlyDelete",
                        "wafer=" + wafer.WaferId,
                        "from=" + beforeLocation,
                        "to=" + wafer.CurrentLocation,
                        "user=" + (userName ?? ""),
                        "noMotion=true");
                    compactionResult = CompactMaterialStateNoLock();
                }

                LogMaterialCompaction("DataOnlyManualDelete", compactionResult);
                if (removedDieCount > 0)
                {
                    Log.Write("Main", "SYSTEM", "MaterialStateService",
                        "DATA ONLY 삭제 위치의 Die Material을 정리했습니다. location=" + location.Kind +
                        ", removedDies=" + removedDieCount + " - Ok");
                }
                NotifyAndSave("DataOnlyManualDelete");
                result.PersistenceSucceeded = TryFlushPendingSave("DataOnlyManualDelete");
                if (!result.PersistenceSucceeded)
                {
                    result.Success = false;
                    result.FailureCode = "DATA-ONLY-DELETE-SAVE-FAIL";
                    result.FailureMessage =
                        "Material 데이터는 메모리에서 변경되었지만 Snapshot 저장을 확인하지 못했습니다. " +
                        "재기동 전에 저장 상태를 확인하십시오.";
                    Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "DataOnlyMaterial",
                        "[DATA ONLY] Material 데이터 삭제 후 Snapshot 저장 확인 실패. material=" +
                        result.MaterialId + ", location=" + result.SourceText + " - Failed");
                    return result;
                }
                result.Success = true;

                Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "DataOnlyMaterial",
                    "[DATA ONLY] Material 데이터 삭제 완료(장비 무동작, 실물 제거 아님). material=" + result.MaterialId +
                    ", location=" + result.SourceText +
                    ", persisted=" + result.PersistenceSucceeded + " - Ok");
                return result;
            }
            catch (Exception ex)
            {
                return FailDataOnly(operation, location, null, "DATA-ONLY-DELETE-EX",
                    "DATA ONLY 삭제 중 예외가 발생했습니다: " + ex.Message, userName);
            }
            finally
            {
            }
        }

        private static bool TryCollectDataOnlyMoveDiesNoLock(
            WaferMaterial wafer,
            DataOnlyLocation source,
            out List<DieMaterial> moveDies,
            out string reason)
        {
            moveDies = new List<DieMaterial>();
            reason = "";
            if (wafer == null || source == null || State == null || State.Dies == null)
                return true;

            bool outputSystem = ManualMaterialPositionService.IsOutputSystemKind(source.Kind);
            List<DieMaterial> relatedDies;
            bool collected = outputSystem
                ? TryCollectOutputParentDiesNoLock(
                    new List<WaferMaterial> { wafer },
                    out relatedDies,
                    out reason)
                : TryCollectInputParentDiesNoLock(
                    new List<WaferMaterial> { wafer },
                    out relatedDies,
                    out reason);
            if (!collected)
                return false;

            foreach (DieMaterial die in relatedDies)
            {
                bool hasActiveReservation =
                    die.ReservedPickerLocation == MaterialLocationKind.PickerFront ||
                    die.ReservedPickerLocation == MaterialLocationKind.PickerRear ||
                    die.ReservedPickerNo > 0;
                bool hasOutputParent =
                    !string.IsNullOrWhiteSpace(die.OutputWaferInstanceId) ||
                    !string.IsNullOrWhiteSpace(die.WaferID_Output);
                bool atSource = IsDataOnlyDieAtLocationNoLock(die, source);
                bool atTransitionalFeederSource = IsDataOnlyTransitionalFeederDieNoLock(
                    die,
                    wafer,
                    source);
                bool atMovableSource = atSource || atTransitionalFeederSource;

                if (!outputSystem && hasOutputParent)
                {
                    if (hasActiveReservation || atMovableSource)
                    {
                        reason = "Input Material 이동 대상 Die가 Output parent/Picker 예약을 유지하고 있습니다. die=" +
                                 (die.DieId ?? "") + ", location=" +
                                 (die.CurrentLocation != null ? die.CurrentLocation.ToString() : "Unknown");
                        moveDies.Clear();
                        return false;
                    }
                    continue;
                }

                if (hasActiveReservation)
                {
                    reason = "DATA ONLY 이동 대상 Die에 Picker 예약이 있습니다. die=" +
                             (die.DieId ?? "") + ", reserved=" +
                             die.ReservedPickerLocation + "/" + die.ReservedPickerNo;
                    moveDies.Clear();
                    return false;
                }

                MaterialLocationKind locationKind = die.CurrentLocation != null
                    ? die.CurrentLocation.Kind
                    : MaterialLocationKind.Unknown;
                if (locationKind == MaterialLocationKind.Unknown)
                    continue;

                if (!atMovableSource)
                {
                    reason = "DATA ONLY 이동 대상 Wafer와 Die의 현재 위치가 일치하지 않습니다. wafer=" +
                             (wafer.WaferId ?? "") + ", die=" + (die.DieId ?? "") +
                             ", waferLocation=" + source.DisplayText +
                             ", dieLocation=" + (die.CurrentLocation != null ? die.CurrentLocation.ToString() : "Unknown");
                    moveDies.Clear();
                    return false;
                }

                moveDies.Add(die);
            }

            return true;
        }

        private static bool IsDataOnlyTransitionalFeederDieNoLock(
            DieMaterial die,
            WaferMaterial wafer,
            DataOnlyLocation source)
        {
            if (die == null || die.CurrentLocation == null || wafer == null || source == null)
                return false;

            if (source.Kind == MaterialLocationKind.InputFeeder)
                return die.CurrentLocation.Kind == MaterialLocationKind.InputStage;
            if (source.Kind != MaterialLocationKind.OutputFeeder)
                return false;

            MaterialLocationKind expectedStage =
                wafer.OutputGrade == DieResult.NG || wafer.OutputCassetteRole == CassetteMaterialRole.Ng1
                    ? MaterialLocationKind.OutputStageNg
                    : MaterialLocationKind.OutputStageGood;
            return die.CurrentLocation.Kind == expectedStage;
        }

        private static bool IsDataOnlyDieAtLocationNoLock(
            DieMaterial die,
            DataOnlyLocation location)
        {
            if (die == null || die.CurrentLocation == null || location == null)
                return false;
            if (die.CurrentLocation.Kind != location.Kind)
                return false;
            if (!location.IsCassette)
                return true;

            return die.CurrentLocation.CassetteRole == location.CassetteRole &&
                   die.CurrentLocation.SlotNumber == location.SlotIndex;
        }

        private static void SynchronizeDataOnlyDieLocationsNoLock(
            ICollection<DieMaterial> moveDies,
            DataOnlyLocation destination)
        {
            if (moveDies == null || destination == null)
                return;

            DateTime updatedAt = DateTime.Now;
            foreach (DieMaterial die in moveDies.Where(item => item != null).Distinct())
            {
                die.CurrentLocation = destination.ToMaterialLocation();
                die.UpdatedAt = updatedAt;
            }
        }

        // To do: [DATA ONLY 교환] Source/Destination 양방향 배치를 한 함수로 처리한다(교환 시 대칭 적용).
        /// <summary>
        /// DATA ONLY 배치 반영입니다. 카세트면 슬롯 pointer와 출력 키를 등록하고, 스테이션이면 위치만 갱신합니다.
        /// 호출 전에 해당 Material의 기존 슬롯 pointer가 제거되어 있어야 합니다(_stateSync 보유 상태에서 호출).
        /// Material ID/검사결과/DieMap은 유지하고, State와 Cassette 메타데이터는
        /// 실제 Auto 이송 완료 상태와 동일하게 목적지 기준으로 맞춥니다.
        /// </summary>
        private static void ApplyDataOnlyPlacementNoLock(WaferMaterial wafer, DataOnlyLocation location)
        {
            if (wafer == null || location == null)
                return;

            if (location.IsCassette)
            {
                CassetteMaterial cassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == location.CassetteRole);
                if (cassette != null)
                {
                    cassette.EnsureSlots();
                    if (location.SlotIndex >= 0 && location.SlotIndex < cassette.Slots.Count)
                    {
                        CassetteSlotMaterial slot = cassette.Slots[location.SlotIndex];
                        if (slot != null)
                        {
                            slot.WaferId = wafer.WaferId;
                            slot.WaferInstanceId = EnsureWaferInstanceIdNoLock(wafer);
                            slot.HasWafer = true;
                        }
                    }

                    if (IsOutputCassetteRole(location.CassetteRole))
                    {
                        wafer.OutputCassetteId = cassette.CassetteId;
                        wafer.OutputCassetteRole = location.CassetteRole;
                        wafer.OutputSlotNumber = location.SlotIndex;
                        wafer.OutputGrade = location.CassetteRole == CassetteMaterialRole.Ng1
                            ? DieResult.NG
                            : DieResult.Good;
                    }

                    // DATA ONLY는 실제 자재 위치를 기준 정보로 복구하는 기능이다.
                    // 다른 Cassette/Slot로 옮긴 경우 다음 Auto가 이전 Source를 다시 참조하지 않도록
                    // 원본 Cassette 정보도 현재 목적지에 맞춘다.
                    wafer.SourceCassetteId = cassette.CassetteId;
                    wafer.SourceCassetteRole = location.CassetteRole;
                    wafer.SourceSlotNumber = location.SlotIndex;
                    wafer.SourceCassetteSlotPosition = double.NaN;

                    // 목적 Cassette LOT가 등록되어 있으면 그 LOT를 기준으로 맞춘다.
                    // Cassette LOT가 비어 있으면 기존 Material LOT를 Cassette에 승계한다.
                    if (!string.IsNullOrWhiteSpace(cassette.CassetteLotId))
                        wafer.CassetteLotId = cassette.CassetteLotId;
                    else if (!string.IsNullOrWhiteSpace(wafer.CassetteLotId))
                        cassette.CassetteLotId = wafer.CassetteLotId;
                }

                wafer.CurrentLocation = location.ToMaterialLocation();

                // 저장된 물리 슬롯 위치는 새 슬롯 기준으로 신뢰할 수 없다.
                // 물리 이동 목표는 중앙 Unit 계산기가 재계산하므로 여기서는 무효화만 한다.
                wafer.CurrentCassetteSlotPosition = double.NaN;
            }
            else
            {
                wafer.CurrentLocation = location.ToMaterialLocation();

                // Output Stage의 GOOD/NG 구분은 실제 배치 위치가 기준이다.
                // DATA ONLY로 Stage 위치를 복구할 때 Grade가 반대로 남아 다음 언로드 대상이
                // 틀어지지 않도록 Stage 종류와 함께 정규화한다.
                if (location.Kind == MaterialLocationKind.OutputStageGood)
                    wafer.OutputGrade = DieResult.Good;
                else if (location.Kind == MaterialLocationKind.OutputStageNg)
                    wafer.OutputGrade = DieResult.NG;
            }

            // Auto 정상 이송과 동일한 목적지 상태:
            // Cassette 반환 완료=Finish, Feeder 이송 중=WorkReady, Stage 공정 중=Working.
            wafer.State = ResolveDataOnlyTargetState(location.Kind);
            wafer.UpdatedAt = DateTime.Now;
        }

        private static WaferMaterialState ResolveDataOnlyTargetState(MaterialLocationKind kind)
        {
            switch (kind)
            {
                case MaterialLocationKind.InputCassette:
                case MaterialLocationKind.OutputCassette:
                    return WaferMaterialState.Finish;

                case MaterialLocationKind.InputFeeder:
                case MaterialLocationKind.OutputFeeder:
                    return WaferMaterialState.WorkReady;

                case MaterialLocationKind.InputStage:
                case MaterialLocationKind.OutputStageGood:
                case MaterialLocationKind.OutputStageNg:
                    return WaferMaterialState.Working;

                default:
                    throw new InvalidOperationException(
                        "DATA ONLY 목적지의 Material State를 결정할 수 없습니다. kind=" + kind);
            }
        }

        // DATA ONLY 대상 위치에서 Material을 정확히 하나 찾는다.
        // 포인터 불일치/중복이 있으면 조용히 하나를 선택하지 않고 실패를 반환한다.
        private static bool TryFindSingleWaferAtDataOnlyLocation(
            DataOnlyLocation location,
            out WaferMaterial wafer,
            out string failureCode,
            out string failureMessage)
        {
            wafer = null;
            failureCode = "";
            failureMessage = "";

            if (location.IsCassette)
            {
                var cassette = State.Cassettes.FirstOrDefault(c => c != null && c.Role == location.CassetteRole);
                if (cassette == null)
                {
                    failureCode = "DATA-ONLY-SOURCE-CASSETTE";
                    failureMessage = "Source 카세트 상태 데이터가 없습니다. role=" + location.CassetteRole;
                    return false;
                }

                cassette.EnsureSlots();
                if (location.SlotIndex < 0 || location.SlotIndex >= cassette.Slots.Count)
                {
                    failureCode = "DATA-ONLY-SOURCE-SLOT-RANGE";
                    failureMessage = "Source Slot이 카세트 범위를 벗어났습니다. role=" + location.CassetteRole +
                                     ", slot=" + (location.SlotIndex + 1) + ", slotCount=" + cassette.Slots.Count;
                    return false;
                }

                var slot = cassette.Slots[location.SlotIndex];
                if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                {
                    failureCode = "DATA-ONLY-SOURCE-EMPTY";
                    failureMessage = "Source 위치에 Material 데이터가 없습니다. " + location.DisplayText;
                    return false;
                }

                string slotReason;
                var slotWafer = ResolveCassetteSlotWaferNoLock(slot, out slotReason);
                if (slotWafer == null)
                {
                    failureCode = "DATA-ONLY-SOURCE-MISMATCH";
                    failureMessage = "Source Slot이 가리키는 Material 객체가 없습니다. " + location.DisplayText +
                                     ", waferId=" + slot.WaferId +
                                     ", reason=" + slotReason;
                    return false;
                }

                if (WaferMaterialStateText.Normalize(slotWafer.State) == WaferMaterialState.Empty)
                {
                    failureCode = "DATA-ONLY-SOURCE-EMPTY-STATE";
                    failureMessage = "Source Material 상태가 EMPTY입니다. " + location.DisplayText +
                                     ", waferId=" + slotWafer.WaferId;
                    return false;
                }

                MaterialLocation expected = location.ToMaterialLocation();
                if (!IsSameMaterialLocation(slotWafer.CurrentLocation, expected))
                {
                    failureCode = "DATA-ONLY-SOURCE-MISMATCH";
                    failureMessage = "Source Slot pointer와 Material 현재 위치가 불일치합니다. " + location.DisplayText +
                                     ", waferId=" + slotWafer.WaferId +
                                     ", currentLocation=" + slotWafer.CurrentLocation;
                    return false;
                }

                WaferMaterial duplicate = FindOtherWaferAtLocation(slotWafer, expected);
                if (duplicate != null)
                {
                    failureCode = "DATA-ONLY-SOURCE-DUPLICATE";
                    failureMessage = "Source 위치를 가리키는 Material이 둘 이상입니다. " + location.DisplayText +
                                     ", wafer1=" + slotWafer.WaferId + ", wafer2=" + duplicate.WaferId;
                    return false;
                }

                wafer = slotWafer;
                return true;
            }

            var candidates = State.Wafers
                .Where(w => w != null &&
                            w.CurrentLocation != null &&
                            w.CurrentLocation.Kind == location.Kind &&
                            WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty)
                .ToList();

            if (candidates.Count == 0)
            {
                failureCode = "DATA-ONLY-SOURCE-EMPTY";
                failureMessage = "Source 위치에 Material 데이터가 없습니다. " + location.DisplayText;
                return false;
            }

            if (candidates.Count > 1)
            {
                failureCode = "DATA-ONLY-SOURCE-DUPLICATE";
                failureMessage = "Source 위치에 Material 데이터가 둘 이상입니다. " + location.DisplayText +
                                 ", materials=" + string.Join(",", candidates.Select(w => w.WaferId).ToArray());
                return false;
            }

            wafer = candidates[0];
            return true;
        }

        private static DataOnlyOperationResult FailDataOnly(
            string operation,
            DataOnlyLocation source,
            DataOnlyLocation destination,
            string failureCode,
            string failureMessage,
            string userName)
        {
            var result = DataOnlyOperationResult.Fail(operation, failureCode, failureMessage);
            result.SourceText = source != null ? source.DisplayText : "";
            result.DestinationText = destination != null ? destination.DisplayText : "";
            Log.Write("Main", string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName, "DataOnlyMaterial",
                "[DATA ONLY] " + operation + " 실패. source=" + result.SourceText +
                ", destination=" + result.DestinationText +
                ", code=" + failureCode +
                ", reason=" + failureMessage + " - Failed");
            return result;
        }

    }
}
