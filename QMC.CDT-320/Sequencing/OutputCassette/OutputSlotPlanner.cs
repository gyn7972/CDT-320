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
                    SlotIndex = sourceSlot
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
            plan = null;

            if (side == BinSide.Ng)
                return TryResolveFirstReady(CassetteMaterialRole.Ng1, TargetCassette.Ng, BinSide.Ng, out plan);

            if (TryResolveFirstReady(CassetteMaterialRole.Good1, TargetCassette.Good1, BinSide.Good, out plan))
                return true;

            return TryResolveFirstReady(CassetteMaterialRole.Good2, TargetCassette.Good2, BinSide.Good, out plan);
        }

        public static bool TryResolveFirstEmpty(CassetteMaterialRole role, TargetCassette target, BinSide side, out OutputSlotPlan plan)
        {
            plan = null;
            try
            {
                var cassette = MaterialStateService.State != null && MaterialStateService.State.Cassettes != null
                    ? MaterialStateService.State.Cassettes.FirstOrDefault(c => c.Role == role)
                    : null;
                if (cassette == null)
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
                            SlotIndex = i
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
                if (cassette == null)
                    return false;

                cassette.EnsureSlots();
                for (int i = 0; i < cassette.Slots.Count; i++)
                {
                    var slot = cassette.Slots[i];
                    if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                        continue;

                    var wafer = MaterialStateService.GetWaferInCassette(role, i);
                    WaferMaterialState state = wafer != null
                        ? WaferMaterialStateText.Normalize(wafer.State)
                        : WaferMaterialState.Ready;
                    if (state != WaferMaterialState.Ready && state != WaferMaterialState.WorkReady)
                        continue;

                    plan = new OutputSlotPlan
                    {
                        Side = side,
                        CassetteRole = role,
                        TargetCassette = target,
                        SlotIndex = i
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
            if (cassette == null || !cassette.IsMapped)
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
            if (slot == null || !slot.HasWafer || string.IsNullOrWhiteSpace(slot.WaferId))
                return true;

            var slotWafer = MaterialStateService.GetWaferInCassette(role, slotIndex);
            WaferMaterialState state = slotWafer != null
                ? WaferMaterialStateText.Normalize(slotWafer.State)
                : WaferMaterialState.Ready;

            if (state == WaferMaterialState.Empty)
                return true;

            if (!string.IsNullOrWhiteSpace(waferId) && string.Equals(slot.WaferId, waferId, StringComparison.OrdinalIgnoreCase))
                return true;

            reason = "Output cassette same source slot is occupied. cassette=" + role + ", slot=" + (slotIndex + 1).ToString("00") + ", wafer=" + slot.WaferId;
            return false;
        }
    }
}
