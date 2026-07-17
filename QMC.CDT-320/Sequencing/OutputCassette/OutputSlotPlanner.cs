using System;
using System.Linq;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    public sealed class OutputSlotPlan
    {
        public BinSide Side { get; set; }
        public CassetteMaterialRole CassetteRole { get; set; }
        public TargetCassette TargetCassette { get; set; }
        public int SlotIndex { get; set; }
        public string WaferId { get; set; }
    }

    public static class OutputSlotPlanner
    {
        public static bool TryResolveNextStoreSlot(DieGrade grade, out OutputSlotPlan plan)
        {
            string reason;
            return TryResolveNextStoreSlot(grade, out plan, out reason);
        }

        public static bool TryResolveNextStoreSlot(DieGrade grade, out OutputSlotPlan plan, out string reason)
        {
            plan = null;
            reason = "";

            try
            {
                MaterialLocationKind stageKind = grade == DieGrade.Ng
                    ? MaterialLocationKind.OutputStageNg
                    : MaterialLocationKind.OutputStageGood;

                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(stageKind);
                if (wafer == null)
                {
                    reason = "Output stage wafer data does not exist. grade=" + grade;
                    return false;
                }

                CassetteMaterialRole sourceRole = wafer.SourceCassetteRole;
                int sourceSlot = wafer.SourceSlotNumber;
                if (sourceSlot < 0)
                {
                    reason = "Output wafer source slot is invalid. wafer=" + wafer.WaferId;
                    return false;
                }

                if (!IsRoleAllowedForGrade(grade, sourceRole))
                {
                    reason = "Output wafer source cassette does not match grade. wafer=" + wafer.WaferId + ", source=" + sourceRole + ", grade=" + grade;
                    return false;
                }

                TargetCassette target;
                BinSide side;
                if (!TryResolveTarget(sourceRole, out target, out side))
                {
                    reason = "Output wafer source cassette is not supported. source=" + sourceRole;
                    return false;
                }

                if (!IsSlotStoreAvailable(sourceRole, sourceSlot, wafer.WaferId, out reason))
                    return false;

                plan = new OutputSlotPlan
                {
                    Side = side,
                    CassetteRole = sourceRole,
                    TargetCassette = target,
                    SlotIndex = sourceSlot,
                    WaferId = wafer.WaferId
                };
                return true;
            }
            catch (Exception ex)
            {
                reason = "Output slot plan failed: " + ex.Message;
                plan = null;
                return false;
            }
            finally
            {
            }
        }

        public static bool TryResolveNextSupplySlot(BinSide side, out OutputSlotPlan plan)
        {
            string reason;
            return TryResolveNextSupplySlot(side, out plan, out reason);
        }

        public static bool TryResolveNextSupplySlot(BinSide side, out OutputSlotPlan plan, out string reason)
        {
            plan = null;
            reason = "";

            if (!ValidateSupplyCassetteConsistency(side, out reason))
                return false;

            if (side == BinSide.Ng)
            {
                bool resolved = TryResolveFirstReady(CassetteMaterialRole.Ng1, TargetCassette.Ng, BinSide.Ng, out plan);
                if (!resolved)
                    reason = "NG 출력 카세트에 Ready 상태의 공급 가능한 Bin이 없습니다.";
                return resolved;
            }

            if (TryResolveFirstReady(CassetteMaterialRole.Good1, TargetCassette.Good1, BinSide.Good, out plan))
                return true;

            if (TryResolveFirstReady(CassetteMaterialRole.Good2, TargetCassette.Good2, BinSide.Good, out plan))
                return true;

            reason = "GOOD 출력 카세트 Good1/Good2에 Ready 상태의 공급 가능한 Bin이 없습니다.";
            return false;
        }

        public static bool ValidateSupplyCassetteConsistency(BinSide side, out string reason)
        {
            reason = "";
            try
            {
                if (side == BinSide.Ng)
                    return ValidateCassetteConsistency(CassetteMaterialRole.Ng1, out reason);

                if (!ValidateCassetteConsistency(CassetteMaterialRole.Good1, out reason))
                    return false;
                return ValidateCassetteConsistency(CassetteMaterialRole.Good2, out reason);
            }
            catch (Exception ex)
            {
                reason = "출력 카세트 센서/Material 일관성 확인 중 예외가 발생했습니다. side=" + side + ", error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        public static bool TryResolveFirstEmpty(CassetteMaterialRole role, TargetCassette target, BinSide side, out OutputSlotPlan plan)
        {
            plan = null;
            try
            {
                var cassette = MaterialStateService.State != null && MaterialStateService.State.Cassettes != null
                    ? MaterialStateService.State.Cassettes.FirstOrDefault(c => c.Role == role)
                    : null;
                if (cassette == null || !cassette.IsEnabled || !cassette.IsPresent || !cassette.IsMapped)
                    return false;

                cassette.EnsureSlots();
                for (int i = 0; i < cassette.Slots.Count; i++)
                {
                    string reason;
                    if (IsSlotStoreAvailable(role, i, "", out reason))
                    {
                        plan = new OutputSlotPlan
                        {
                            Side = side,
                            CassetteRole = role,
                            TargetCassette = target,
                            SlotIndex = i,
                            WaferId = ""
                        };
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                plan = null;
                return false;
            }
            finally
            {
            }
        }

        public static bool TryResolveFirstReady(CassetteMaterialRole role, TargetCassette target, BinSide side, out OutputSlotPlan plan)
        {
            plan = null;
            try
            {
                var cassette = MaterialStateService.State != null && MaterialStateService.State.Cassettes != null
                    ? MaterialStateService.State.Cassettes.FirstOrDefault(c => c.Role == role)
                    : null;
                if (cassette == null || !cassette.IsEnabled || !cassette.IsPresent || !cassette.IsMapped)
                    return false;

                cassette.EnsureSlots();
                for (int i = 0; i < cassette.Slots.Count; i++)
                {
                    var slot = cassette.Slots[i];
                    if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                        continue;

                    var wafer = MaterialStateService.GetWaferInCassette(role, i);
                    if (wafer == null)
                        continue;

                    WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
                    if (state != WaferMaterialState.Ready)
                        continue;

                    plan = new OutputSlotPlan
                    {
                        Side = side,
                        CassetteRole = role,
                        TargetCassette = target,
                        SlotIndex = i,
                        WaferId = wafer.WaferId
                    };
                    return true;
                }

                return false;
            }
            catch
            {
                plan = null;
                return false;
            }
            finally
            {
            }
        }

        private static bool IsRoleAllowedForGrade(DieGrade grade, CassetteMaterialRole role)
        {
            if (grade == DieGrade.Ng)
                return role == CassetteMaterialRole.Ng1;

            return role == CassetteMaterialRole.Good1 || role == CassetteMaterialRole.Good2;
        }

        private static bool TryResolveTarget(CassetteMaterialRole role, out TargetCassette target, out BinSide side)
        {
            switch (role)
            {
                // GOOD 1단 카세트 처리
                case CassetteMaterialRole.Good1:
                    target = TargetCassette.Good1;
                    side = BinSide.Good;
                    return true;
                // GOOD 2단 카세트 처리
                case CassetteMaterialRole.Good2:
                    target = TargetCassette.Good2;
                    side = BinSide.Good;
                    return true;
                // NG 1단 카세트 처리
                case CassetteMaterialRole.Ng1:
                    target = TargetCassette.Ng;
                    side = BinSide.Ng;
                    return true;
                default:
                    target = TargetCassette.Good1;
                    side = BinSide.Good;
                    return false;
            }
        }

        private static bool IsSlotStoreAvailable(CassetteMaterialRole role, int slotIndex, string waferId, out string reason)
        {
            reason = "";
            var cassette = MaterialStateService.State != null && MaterialStateService.State.Cassettes != null
                ? MaterialStateService.State.Cassettes.FirstOrDefault(c => c.Role == role)
                : null;
            if (cassette == null || !cassette.IsEnabled || !cassette.IsPresent || !cassette.IsMapped)
            {
                reason = "Output cassette is not mapped. cassette=" + role;
                return false;
            }

            cassette.EnsureSlots();
            if (slotIndex < 0 || slotIndex >= cassette.Slots.Count)
            {
                reason = "Output cassette source slot is out of range. cassette=" + role + ", slot=" + (slotIndex + 1).ToString("00");
                return false;
            }

            var slot = cassette.Slots[slotIndex];
            if (slot == null)
            {
                reason = "Output cassette slot data is missing. cassette=" + role + ", slot=" + (slotIndex + 1).ToString("00");
                return false;
            }

            bool hasWaferId = !string.IsNullOrWhiteSpace(slot.WaferId);
            if (slot.HasWafer != hasWaferId)
            {
                reason = "Output cassette sensor/slot data mismatch. cassette=" + role +
                         ", slot=" + (slotIndex + 1).ToString("00") +
                         ", hasWafer=" + slot.HasWafer + ", waferId=" + slot.WaferId;
                return false;
            }

            WaferMaterial logicalWafer = FindWaferAtCassetteSlot(role, slotIndex);
            if (!slot.HasWafer)
            {
                if (logicalWafer != null)
                {
                    reason = "Output cassette slot/Material location mismatch. cassette=" + role +
                             ", slot=" + (slotIndex + 1).ToString("00") +
                             ", materialWafer=" + logicalWafer.WaferId;
                    return false;
                }
                return true;
            }

            var slotWafer = MaterialStateService.GetWaferInCassette(role, slotIndex);
            if (slotWafer == null)
            {
                reason = "Output cassette occupied slot has no Material data. cassette=" + role +
                         ", slot=" + (slotIndex + 1).ToString("00") + ", wafer=" + slot.WaferId;
                return false;
            }

            WaferMaterialState state = WaferMaterialStateText.Normalize(slotWafer.State);

            if (state == WaferMaterialState.Empty)
            {
                reason = "Output cassette occupied slot Material state is Empty. cassette=" + role +
                         ", slot=" + (slotIndex + 1).ToString("00") + ", wafer=" + slot.WaferId;
                return false;
            }

            reason = "Output cassette source slot is occupied and cannot be overwritten. cassette=" + role +
                     ", slot=" + (slotIndex + 1).ToString("00") +
                     ", slotWafer=" + slot.WaferId + ", movingWafer=" + waferId;
            return false;
        }

        private static bool ValidateCassetteConsistency(CassetteMaterialRole role, out string reason)
        {
            reason = "";
            var cassette = MaterialStateService.State != null && MaterialStateService.State.Cassettes != null
                ? MaterialStateService.State.Cassettes.FirstOrDefault(c => c.Role == role)
                : null;
            if (cassette == null || !cassette.IsEnabled || !cassette.IsPresent || !cassette.IsMapped)
                return true;

            if (cassette.SlotCount <= 0 || cassette.Slots == null || cassette.Slots.Count != cassette.SlotCount)
            {
                reason = "출력 카세트 SlotCount와 Material slot 수가 일치하지 않습니다. cassette=" + role +
                         ", slotCount=" + cassette.SlotCount +
                         ", materialSlots=" + (cassette.Slots != null ? cassette.Slots.Count : 0);
                return false;
            }

            var duplicateSlotWafer = cassette.Slots
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.WaferId))
                .GroupBy(s => s.WaferId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateSlotWafer != null)
            {
                reason = "동일한 Output Bin ID가 둘 이상의 cassette slot에 등록되어 있습니다. cassette=" + role +
                         ", wafer=" + duplicateSlotWafer.Key;
                return false;
            }

            for (int i = 0; i < cassette.Slots.Count; i++)
            {
                CassetteSlotMaterial slot = cassette.Slots[i];
                if (slot == null)
                {
                    reason = "출력 카세트 slot 데이터가 없습니다. cassette=" + role + ", slot=" + (i + 1).ToString("00");
                    return false;
                }

                bool hasWaferId = !string.IsNullOrWhiteSpace(slot.WaferId);
                if (slot.HasWafer != hasWaferId)
                {
                    reason = "출력 카세트 센서/slot 데이터가 불일치합니다. cassette=" + role +
                             ", slot=" + (i + 1).ToString("00") +
                             ", hasWafer=" + slot.HasWafer + ", waferId=" + slot.WaferId;
                    return false;
                }

                var locationWafers = MaterialStateService.State.Wafers
                    .Where(w => IsWaferAtOutputCassetteSlot(w, role, i))
                    .ToList();
                if (locationWafers.Count > 1)
                {
                    reason = "하나의 Output cassette slot에 둘 이상의 Material 위치가 등록되어 있습니다. cassette=" + role +
                             ", slot=" + (i + 1).ToString("00") +
                             ", wafers=" + string.Join(",", locationWafers.Select(w => w.WaferId));
                    return false;
                }
                WaferMaterial locationWafer = locationWafers.FirstOrDefault();
                if (!slot.HasWafer)
                {
                    if (locationWafer != null)
                    {
                        reason = "출력 카세트 빈 slot에 Material 위치 데이터가 남아 있습니다. cassette=" + role +
                                 ", slot=" + (i + 1).ToString("00") + ", wafer=" + locationWafer.WaferId;
                        return false;
                    }
                    continue;
                }

                WaferMaterial wafer = MaterialStateService.GetWaferInCassette(role, i);
                if (wafer == null)
                {
                    reason = "출력 카세트 점유 slot의 Material 데이터가 없습니다. cassette=" + role +
                             ", slot=" + (i + 1).ToString("00") + ", wafer=" + slot.WaferId;
                    return false;
                }

                if (locationWafer == null || !string.Equals(locationWafer.WaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "출력 카세트 slot과 Material 위치가 불일치합니다. cassette=" + role +
                             ", slot=" + (i + 1).ToString("00") + ", wafer=" + wafer.WaferId;
                    return false;
                }

                if (wafer.SourceCassetteRole != role || wafer.SourceSlotNumber != i)
                {
                    reason = "출력 Bin의 원본 cassette/slot 정보가 현재 slot과 다릅니다. cassette=" + role +
                             ", slot=" + (i + 1).ToString("00") + ", wafer=" + wafer.WaferId +
                             ", sourceRole=" + wafer.SourceCassetteRole +
                             ", sourceSlot=" + (wafer.SourceSlotNumber + 1).ToString("00");
                    return false;
                }

                WaferMaterialState state = WaferMaterialStateText.Normalize(wafer.State);
                if (state != WaferMaterialState.Ready && state != WaferMaterialState.Finish)
                {
                    reason = "출력 카세트 점유 slot의 Material 상태가 허용되지 않습니다. cassette=" + role +
                             ", slot=" + (i + 1).ToString("00") + ", wafer=" + wafer.WaferId +
                             ", state=" + state;
                    return false;
                }
            }

            return true;
        }

        private static WaferMaterial FindWaferAtCassetteSlot(CassetteMaterialRole role, int slotIndex)
        {
            if (MaterialStateService.State == null || MaterialStateService.State.Wafers == null)
                return null;

            return MaterialStateService.State.Wafers.FirstOrDefault(w => IsWaferAtOutputCassetteSlot(w, role, slotIndex));
        }

        private static bool IsWaferAtOutputCassetteSlot(WaferMaterial wafer, CassetteMaterialRole role, int slotIndex)
        {
            return wafer != null &&
                   wafer.CurrentLocation != null &&
                   wafer.CurrentLocation.Kind == MaterialLocationKind.OutputCassette &&
                   wafer.CurrentLocation.CassetteRole == role &&
                   wafer.CurrentLocation.SlotNumber == slotIndex &&
                   WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty;
        }
    }
}
