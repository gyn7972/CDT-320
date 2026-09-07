using System;
using System.Collections.Generic;
using System.Linq;
using QMC.CDT320.Materials;

// 저장, 로그, 캐시 무효화만 메모리 대역을 사용한다. Clear/관계 확인/compaction은 생산 코드다.
// 이 실행 파일에는 MaterialStorage, Machine, Controller, Unit 및 장비 SDK 참조가 없다.
namespace QMC.Common
{
    public enum LogLevel { AboveNormal }
    public static class Log
    {
        public static void Write(LogLevel level, string category, string source, string message) { }
    }
}

namespace QMC.Common.Logging
{
    public enum EventKind { Warning }
    public static class EventLogger
    {
        public static void Write(EventKind kind, string user, string code, string message) { }
    }
}

namespace QMC.CDT320.Materials
{
    // Wafer 모델의 무관한 Review 직렬화 필드: 본 테스트에서 생성하거나 사용하지 않는다.
    public sealed class InputStageReviewSavedVerification { }

    public static partial class MaterialStateService
    {
        private static readonly object _stateSync = new object();
        public static MaterialSnapshot State { get; set; }
        public static int SaveRequests { get; set; }
        private static void NotifyAndSave(string reason) { SaveRequests++; }
        private static void InvalidateDieByIdIndexNoLock() { }
        private static void LogMaterialCompaction(string reason, MaterialCompactionResult result) { }
        private static MaterialCompactionResult CompactMaterialStateNoLock()
        {
            return MaterialStateCompactor.Compact(State);
        }
    }
}

internal static class InputCassetteClearTests
{
    private static int _assertions;
    private static int _cases;
    private static int _failures;

    private static void Require(bool value, string message)
    {
        _assertions++;
        if (!value) throw new InvalidOperationException(message);
    }

    private static void Case(string name, Action test)
    {
        _cases++;
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception ex) { _failures++; Console.Error.WriteLine("FAIL " + name + ": " + ex.Message); }
    }

    private static CassetteMaterial Cassette(CassetteMaterialRole role)
    {
        return MaterialStateService.State.Cassettes.Single(c => c.Role == role);
    }

    private static void Reset(bool enableInput2 = false)
    {
        MaterialStateService.State = new MaterialSnapshot { LotId = "OFFLINE-LOT" };
        MaterialStateService.SaveRequests = 0;
        foreach (CassetteMaterialRole role in Enum.GetValues(typeof(CassetteMaterialRole)))
        {
            var cassette = new CassetteMaterial
            {
                CassetteId = "OFFLINE-" + role, Role = role, SlotCount = 3,
                IsEnabled = role != CassetteMaterialRole.Input2 || enableInput2,
                IsMapped = true, IsPresent = true, CassetteLotId = "OFFLINE-LOT"
            };
            cassette.EnsureSlots();
            MaterialStateService.State.Cassettes.Add(cassette);
        }
    }

    private static WaferMaterial Wafer(CassetteMaterialRole role, int slot, WaferMaterialState state = WaferMaterialState.Finish)
    {
        bool output = role == CassetteMaterialRole.Good1 || role == CassetteMaterialRole.Good2 || role == CassetteMaterialRole.Ng1;
        var wafer = new WaferMaterial
        {
            WaferId = "OFFLINE-" + role + "-" + slot,
            WaferInstanceId = "INSTANCE-" + role + "-" + slot,
            State = state, CassetteLotId = "OFFLINE-LOT", SourceCassetteRole = role, SourceSlotNumber = slot,
            CurrentLocation = MaterialLocation.Cassette(output ? MaterialLocationKind.OutputCassette : MaterialLocationKind.InputCassette, role, slot)
        };
        MaterialStateService.State.Wafers.Add(wafer);
        CassetteSlotMaterial pointer = Cassette(role).Slots[slot];
        pointer.WaferId = wafer.WaferId;
        pointer.WaferInstanceId = wafer.WaferInstanceId;
        pointer.HasWafer = state != WaferMaterialState.Empty;
        return wafer;
    }

    private static DieMaterial Die(WaferMaterial wafer, string suffix = "A")
    {
        var die = new DieMaterial
        {
            DieId = wafer.WaferId + "-DIE-" + suffix,
            WaferID_Input = wafer.WaferId, InputWaferInstanceId = wafer.WaferInstanceId,
            CurrentLocation = MaterialLocation.Cassette(MaterialLocationKind.InputCassette,
                wafer.SourceCassetteRole, wafer.SourceSlotNumber)
        };
        MaterialStateService.State.Dies.Add(die);
        wafer.DieIds.Add(die.DieId);
        return die;
    }

    private static void RequireCleared(CassetteMaterialRole role)
    {
        CassetteMaterial cassette = Cassette(role);
        Require(cassette.Slots.All(s => !s.HasWafer && s.WaferId == "" && s.WaferInstanceId == ""), role + " slot pointers must clear");
        Require(!cassette.IsMapped && !cassette.IsPresent && cassette.CassetteLotId == "", role + " must require remapping with no old lot");
    }

    private static void RequireUnchanged(WaferMaterial wafer, DieMaterial die, string reason)
    {
        Require(!string.IsNullOrWhiteSpace(reason), "blocked clear must return a reason");
        Require(MaterialStateService.SaveRequests == 0, "blocked clear must not request save");
        Require(MaterialStateService.State.Wafers.Contains(wafer) && MaterialStateService.State.Dies.Contains(die), "blocked clear must preserve materials");
        Require(Cassette(wafer.SourceCassetteRole).Slots[wafer.SourceSlotNumber].HasWafer, "blocked clear must preserve slot");
    }

    private static void CrossRoleClear(bool both)
    {
        Reset(both);
        Die(Wafer(CassetteMaterialRole.Input1, 0));
        if (both) Die(Wafer(CassetteMaterialRole.Input2, 1));
        string reason;
        Require(MaterialStateService.ClearInputCassetteAllSlotData(out reason), "valid finish clear blocked: " + reason);
        RequireCleared(CassetteMaterialRole.Input1);
        RequireCleared(CassetteMaterialRole.Input2);
        Require(MaterialStateService.State.Dies.Count == 0, "input-only dies must be removed");
        Require(MaterialStateService.SaveRequests == 1, "successful all clear must request one save");
    }

    private static void SingleSlotClear()
    {
        Reset(true);
        WaferMaterial target = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial removed = Die(target);
        WaferMaterial otherSlot = Wafer(CassetteMaterialRole.Input1, 1);
        DieMaterial keptSlotDie = Die(otherSlot);
        WaferMaterial otherRole = Wafer(CassetteMaterialRole.Input2, 2);
        DieMaterial keptRoleDie = Die(otherRole);
        string reason;
        Require(MaterialStateService.ClearInputCassetteSlotData(CassetteMaterialRole.Input1, 0, out reason), "single slot clear blocked: " + reason);
        Require(!Cassette(CassetteMaterialRole.Input1).Slots[0].HasWafer, "target slot must clear");
        Require(!MaterialStateService.State.Dies.Contains(removed), "target input-only die must be removed");
        Require(MaterialStateService.State.Dies.Contains(keptSlotDie) && MaterialStateService.State.Dies.Contains(keptRoleDie), "unrelated dies must remain");
        Require(Cassette(CassetteMaterialRole.Input1).Slots[1].HasWafer && Cassette(CassetteMaterialRole.Input2).Slots[2].HasWafer, "unrelated slots must remain");
        Require(otherSlot.State == WaferMaterialState.Finish && otherRole.State == WaferMaterialState.Finish, "unrelated wafer states must remain");
        Require(Cassette(CassetteMaterialRole.Input1).IsMapped, "occupied cassette must retain mapping");
        Require(MaterialStateService.SaveRequests == 1, "slot clear must save once");
    }

    private static void OrphanIsBlocked(bool emptyRole)
    {
        Reset();
        WaferMaterial wafer = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial die = Die(wafer);
        MaterialStateService.State.Dies.Add(new DieMaterial
        {
            DieId = "ORPHAN", InputWaferInstanceId = "MISSING-PARENT", WaferID_Input = "MISSING",
            CurrentLocation = MaterialLocation.Cassette(MaterialLocationKind.InputCassette,
                emptyRole ? CassetteMaterialRole.Input2 : CassetteMaterialRole.Input1, 0)
        });
        string reason;
        Require(!MaterialStateService.ClearInputCassetteAllSlotData(out reason), "orphan in clear scope must block");
        RequireUnchanged(wafer, die, reason);
    }

    private static void MalformedDieLocationIsBlocked(CassetteMaterialRole role, int slot)
    {
        Reset();
        WaferMaterial wafer = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial die = Die(wafer);
        die.CurrentLocation = MaterialLocation.Cassette(MaterialLocationKind.InputCassette, role, slot);
        string reason;
        Require(!MaterialStateService.ClearInputCassetteAllSlotData(out reason), "malformed or mismatched input die location must block");
        RequireUnchanged(wafer, die, reason);
    }

    private static void OrphanInInvalidCassetteIsBlocked(bool duplicateCassette)
    {
        Reset();
        WaferMaterial wafer = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial die = Die(wafer);
        var orphan = new DieMaterial
        {
            DieId = "ORPHAN-INPUT2", InputWaferInstanceId = "MISSING-PARENT", WaferID_Input = "MISSING",
            CurrentLocation = MaterialLocation.Cassette(MaterialLocationKind.InputCassette, CassetteMaterialRole.Input2, 0)
        };
        MaterialStateService.State.Dies.Add(orphan);
        if (duplicateCassette)
        {
            var duplicate = new CassetteMaterial
            {
                CassetteId = "DUPLICATE-INPUT2", Role = CassetteMaterialRole.Input2,
                SlotCount = 3, IsEnabled = false
            };
            duplicate.EnsureSlots();
            MaterialStateService.State.Cassettes.Add(duplicate);
        }
        else MaterialStateService.State.Cassettes.Remove(Cassette(CassetteMaterialRole.Input2));
        int cassetteCount = MaterialStateService.State.Cassettes.Count;
        string reason;
        Require(!MaterialStateService.CanCompleteInputCassetteExchange(out reason), "orphan in missing or duplicate cassette must block exchange eligibility");
        Require(!MaterialStateService.ClearInputCassetteForExchange(out reason), "orphan in missing or duplicate cassette must block exchange");
        Require(!MaterialStateService.ClearInputCassetteAllSlotData(out reason), "out-of-scope orphan with invalid cassette must block administrative clear");
        RequireUnchanged(wafer, die, reason);
        Require(MaterialStateService.State.Cassettes.Count == cassetteCount, "blocked orphan clear must preserve cassette records");
        Require(MaterialStateService.State.Dies.Contains(orphan) && orphan.CurrentLocation.CassetteRole == CassetteMaterialRole.Input2, "blocked orphan clear must preserve recovery evidence");
    }

    private static void LegacyParent(bool ambiguous)
    {
        Reset(true);
        WaferMaterial wafer = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial die = Die(wafer);
        die.InputWaferInstanceId = "";
        if (ambiguous)
        {
            WaferMaterial duplicate = Wafer(CassetteMaterialRole.Input2, 0);
            duplicate.WaferId = wafer.WaferId;
            Cassette(CassetteMaterialRole.Input2).Slots[0].WaferId = wafer.WaferId;
        }
        string reason;
        bool result = MaterialStateService.ClearInputCassetteAllSlotData(out reason);
        Require(result != ambiguous, "legacy parent must resolve exactly one wafer: " + reason);
        if (ambiguous) RequireUnchanged(wafer, die, reason);
        else RequireCleared(CassetteMaterialRole.Input1);
    }

    private static void SameRoleWrongDieSlotIsBlocked(int slot)
    {
        Reset();
        WaferMaterial wafer = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial die = Die(wafer);
        if (slot == 1) Die(Wafer(CassetteMaterialRole.Input1, 1));
        die.CurrentLocation.SlotNumber = slot;
        string reason;
        Require(!MaterialStateService.CanCompleteInputCassetteExchange(out reason), "die in a different slot from its parent must block exchange eligibility");
        Require(!MaterialStateService.ClearInputCassetteForExchange(out reason), "same-role wrong die slot must block exchange");
        Require(!MaterialStateService.ClearInputCassetteAllSlotData(out reason), "same-role wrong die slot must also block administrative clear");
        RequireUnchanged(wafer, die, reason);
    }

    private static void InvalidWaferPointerIsBlocked(string invalidKind)
    {
        Reset(true);
        WaferMaterial wafer = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial die = Die(wafer);
        CassetteSlotMaterial pointer = Cassette(CassetteMaterialRole.Input1).Slots[0];
        if (invalidKind == "missing")
        {
            pointer.HasWafer = false;
            pointer.WaferId = "";
            pointer.WaferInstanceId = "";
        }
        else if (invalidKind == "wrong")
        {
            WaferMaterial sibling = Wafer(CassetteMaterialRole.Input1, 1);
            pointer.WaferId = sibling.WaferId;
            pointer.WaferInstanceId = sibling.WaferInstanceId;
        }
        else if (invalidKind == "role")
        {
            pointer.HasWafer = false;
            pointer.WaferId = "";
            pointer.WaferInstanceId = "";
            wafer.CurrentLocation.CassetteRole = CassetteMaterialRole.Good1;
        }
        else if (invalidKind == "cassette")
        {
            MaterialStateService.State.Cassettes.Remove(Cassette(CassetteMaterialRole.Input1));
        }
        else throw new InvalidOperationException("unknown pointer fixture");

        int waferCount = MaterialStateService.State.Wafers.Count;
        int cassetteCount = MaterialStateService.State.Cassettes.Count;
        string pointerId = pointer.WaferInstanceId;
        bool occupied = pointer.HasWafer;
        CassetteMaterialRole locationRole = wafer.CurrentLocation.CassetteRole;
        string reason;
        Require(!MaterialStateService.CanCompleteInputCassetteExchange(out reason), "invalid reverse wafer pointer must block eligibility");
        Require(!MaterialStateService.ClearInputCassetteForExchange(out reason), "invalid reverse wafer pointer must block exchange");
        Require(!MaterialStateService.ClearInputCassetteAllSlotData(out reason), "invalid reverse wafer pointer must block administrative clear");
        Require(!string.IsNullOrWhiteSpace(reason), "invalid reverse pointer must return a recovery reason");
        Require(MaterialStateService.SaveRequests == 0, "invalid reverse pointer must not save");
        Require(MaterialStateService.State.Wafers.Count == waferCount && MaterialStateService.State.Cassettes.Count == cassetteCount, "invalid reverse pointer must preserve graph size");
        Require(MaterialStateService.State.Wafers.Contains(wafer) && MaterialStateService.State.Dies.Contains(die), "invalid reverse pointer must preserve original materials");
        Require(wafer.State == WaferMaterialState.Finish && wafer.CurrentLocation.CassetteRole == locationRole, "invalid reverse pointer must preserve wafer recovery state");
        Require(pointer.WaferInstanceId == pointerId && pointer.HasWafer == occupied, "invalid reverse pointer must not silently repair or detach the slot");
    }

    private static void ActiveDieIsBlocked(MaterialLocationKind location, bool reserved, bool outputParent)
    {
        Reset();
        WaferMaterial wafer = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial die = Die(wafer);
        if (location != MaterialLocationKind.InputCassette) die.CurrentLocation = new MaterialLocation { Kind = location, PickerNo = 1 };
        if (reserved) { die.ReservedPickerLocation = MaterialLocationKind.PickerRear; die.ReservedPickerNo = 1; }
        if (outputParent)
        {
            WaferMaterial output = Wafer(CassetteMaterialRole.Good1, 0);
            die.OutputWaferInstanceId = output.WaferInstanceId;
            die.WaferID_Output = output.WaferId;
            output.DieIds.Add(die.DieId);
        }
        string reason;
        Require(!MaterialStateService.ClearInputCassetteForExchange(out reason), "active die must block normal exchange");
        RequireUnchanged(wafer, die, reason);
    }

    private static void OutputTraceabilityIsPreserved()
    {
        Reset();
        WaferMaterial input = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial unpicked = Die(input, "UNPICKED");
        DieMaterial placed = Die(input, "PLACED");
        WaferMaterial output = Wafer(CassetteMaterialRole.Good1, 0);
        placed.WaferID_Output = output.WaferId;
        placed.OutputWaferInstanceId = output.WaferInstanceId;
        placed.CurrentLocation = MaterialLocation.Cassette(MaterialLocationKind.OutputCassette, CassetteMaterialRole.Good1, 0);
        output.DieIds.Add(placed.DieId);
        placed.Inspections.Add(new DieInspectionRecord { InspectionType = "OFFLINE-RESULT", Result = MaterialInspectionResult.Ok });
        string reason;
        Require(MaterialStateService.ClearInputCassetteForExchange(out reason), "output history must not block exchange: " + reason);
        RequireCleared(CassetteMaterialRole.Input1);
        Require(MaterialStateService.State.Wafers.Contains(input), "historical input parent must remain reachable");
        Require(input.State == WaferMaterialState.Empty && input.CurrentLocation.Kind == MaterialLocationKind.Unknown, "historical wafer must be detached from equipment");
        Require(MaterialStateService.State.Dies.Contains(unpicked) && unpicked.CurrentLocation.Kind == MaterialLocationKind.Unknown, "same wafer unpicked history must be retained and detached");
        Require(MaterialStateService.State.Dies.Contains(placed) && placed.CurrentLocation.Kind == MaterialLocationKind.OutputCassette, "placed die must retain output physical location");
        Require(placed.InputWaferInstanceId == input.WaferInstanceId && placed.OutputWaferInstanceId == output.WaferInstanceId, "input/output parent identity must remain");
        Require(placed.Inspections.Count == 1 && placed.Inspections[0].Result == MaterialInspectionResult.Ok, "inspection trace must remain");
        Require(output.State == WaferMaterialState.Finish && Cassette(CassetteMaterialRole.Good1).Slots[0].HasWafer, "output cassette must remain occupied");
    }

    private static void PickerProductDataGateUsesCurrentAndReservationOnly()
    {
        Reset();
        WaferMaterial input = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial placed = Die(input, "OUTPUT-GATE");
        WaferMaterial output = Wafer(CassetteMaterialRole.Good1, 0);
        placed.WaferID_Output = output.WaferId;
        placed.OutputWaferInstanceId = output.WaferInstanceId;
        placed.CurrentLocation = MaterialLocation.Cassette(
            MaterialLocationKind.OutputCassette, CassetteMaterialRole.Good1, 0);
        output.DieIds.Add(placed.DieId);

        string reason;
        Require(MaterialStateService.TryValidatePickerProductDataEmpty(out reason),
            "valid output-side die data must not block picker-empty gate: " + reason);

        placed.CurrentLocation = new MaterialLocation { Kind = MaterialLocationKind.PickerFront, PickerNo = 2 };
        Require(!MaterialStateService.TryValidatePickerProductDataEmpty(out reason) && reason.Contains(placed.DieId),
            "current picker die must block picker-empty gate");

        placed.CurrentLocation = MaterialLocation.Cassette(
            MaterialLocationKind.OutputCassette, CassetteMaterialRole.Good1, 0);
        placed.ReservedPickerLocation = MaterialLocationKind.PickerRear;
        placed.ReservedPickerNo = 3;
        Require(!MaterialStateService.TryValidatePickerProductDataEmpty(out reason) && reason.Contains("reserved"),
            "picker reservation must block picker-empty gate");

        placed.ReservedPickerLocation = MaterialLocationKind.Unknown;
        placed.ReservedPickerNo = -1;
        placed.CurrentLocation = null;
        Require(!MaterialStateService.TryValidatePickerProductDataEmpty(out reason),
            "unknown die location must fail closed");
    }

    private static void FinishGate(WaferMaterialState state)
    {
        Reset();
        WaferMaterial wafer = Wafer(CassetteMaterialRole.Input1, 0, state);
        DieMaterial die = Die(wafer);
        string reason;
        Require(!MaterialStateService.CanCompleteInputCassetteExchange(out reason), "unfinished wafer must block exchange eligibility");
        Require(!MaterialStateService.ClearInputCassetteForExchange(out reason), "unfinished wafer must block exchange mutation");
        RequireUnchanged(wafer, die, reason);
        Require(wafer.State == state, "blocked exchange must preserve process state");
        Require(MaterialStateService.ClearInputCassetteAllSlotData(out reason), "explicit administrative data clear must remain available: " + reason);
    }

    private static void InternalWaferIsBlocked(MaterialLocationKind location)
    {
        Reset();
        WaferMaterial wafer = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial die = Die(wafer);
        WaferMaterial inside = new WaferMaterial
        {
            WaferId = "INTERNAL-FINISHED", WaferInstanceId = "INTERNAL-INSTANCE",
            State = WaferMaterialState.Finish, CurrentLocation = new MaterialLocation { Kind = location }
        };
        MaterialStateService.State.Wafers.Add(inside);
        string reason;
        Require(!MaterialStateService.CanCompleteInputCassetteExchange(out reason), "Finish wafer inside equipment must block");
        Require(!MaterialStateService.ClearInputCassetteForExchange(out reason), "Finish at feeder/stage must never be silently erased");
        RequireUnchanged(wafer, die, reason);
        Require(inside.CurrentLocation.Kind == location && inside.State == WaferMaterialState.Finish, "internal mismatch must remain available for recovery");
    }

    private static void FinishExchangeAndRecheck()
    {
        Reset();
        WaferMaterial wafer = Wafer(CassetteMaterialRole.Input1, 0);
        DieMaterial die = Die(wafer);
        string reason;
        Require(MaterialStateService.CanCompleteInputCassetteExchange(out reason), "valid Finish exchange eligibility blocked: " + reason);
        Require(MaterialStateService.SaveRequests == 0 && wafer.State == WaferMaterialState.Finish, "eligibility check must not clear or save");
        die.ReservedPickerLocation = MaterialLocationKind.PickerFront;
        die.ReservedPickerNo = 1;
        Require(!MaterialStateService.ClearInputCassetteForExchange(out reason), "mutation must revalidate a reservation acquired after eligibility check");
        RequireUnchanged(wafer, die, reason);
        die.ReservedPickerLocation = MaterialLocationKind.Unknown;
        die.ReservedPickerNo = -1;
        Require(MaterialStateService.ClearInputCassetteForExchange(out reason), "Finish exchange must succeed after valid state restored: " + reason);
        RequireCleared(CassetteMaterialRole.Input1);
        Require(MaterialStateService.SaveRequests == 1, "exchange must save once");
        Require(MaterialStateService.CanCompleteInputCassetteExchange(out reason), "already-cleared state must allow a persistence retry: " + reason);
        Require(MaterialStateService.ClearInputCassetteForExchange(out reason), "already-cleared exchange must be idempotent for persistence retry: " + reason);
        RequireCleared(CassetteMaterialRole.Input1);
        Require(MaterialStateService.State.Dies.Count == 0 && MaterialStateService.State.Wafers.Count == 0, "retry must keep input material empty");
        Require(MaterialStateService.SaveRequests == 2, "retry must make one fresh persistence request");
    }

    private static int Main()
    {
        Case("finish_input1_empty_disabled_input2", () => CrossRoleClear(false));
        Case("both_input_roles_with_passive_dies", () => CrossRoleClear(true));
        Case("single_slot_preserves_other_slots_and_role", SingleSlotClear);
        Case("orphan_inside_target_role_blocks_without_mutation", () => OrphanIsBlocked(false));
        Case("orphan_inside_empty_disabled_role_blocks", () => OrphanIsBlocked(true));
        Case("input2_orphan_without_input2_cassette_blocks", () => OrphanInInvalidCassetteIsBlocked(false));
        Case("input2_orphan_with_duplicate_input2_cassettes_blocks", () => OrphanInInvalidCassetteIsBlocked(true));
        Case("input_die_with_output_role_blocks", () => MalformedDieLocationIsBlocked(CassetteMaterialRole.Good1, 0));
        Case("input_die_with_negative_slot_blocks", () => MalformedDieLocationIsBlocked(CassetteMaterialRole.Input1, -1));
        Case("target_die_in_another_input_role_blocks", () => MalformedDieLocationIsBlocked(CassetteMaterialRole.Input2, 0));
        Case("target_die_in_valid_sibling_slot_blocks", () => SameRoleWrongDieSlotIsBlocked(1));
        Case("target_die_in_slot_above_upper_bound_blocks", () => SameRoleWrongDieSlotIsBlocked(99));
        Case("finish_wafer_missing_reverse_pointer_blocks", () => InvalidWaferPointerIsBlocked("missing"));
        Case("finish_wafer_wrong_reverse_pointer_blocks", () => InvalidWaferPointerIsBlocked("wrong"));
        Case("input_cassette_wafer_with_invalid_role_blocks", () => InvalidWaferPointerIsBlocked("role"));
        Case("input_cassette_wafer_without_cassette_blocks", () => InvalidWaferPointerIsBlocked("cassette"));
        Case("unique_legacy_display_parent_is_supported", () => LegacyParent(false));
        Case("ambiguous_legacy_display_parent_blocks", () => LegacyParent(true));
        Case("input_only_die_on_front_picker_blocks", () => ActiveDieIsBlocked(MaterialLocationKind.PickerFront, false, false));
        Case("input_only_die_on_rear_picker_blocks", () => ActiveDieIsBlocked(MaterialLocationKind.PickerRear, false, false));
        Case("picker_reservation_blocks", () => ActiveDieIsBlocked(MaterialLocationKind.InputCassette, true, false));
        Case("output_linked_picker_reservation_blocks", () => ActiveDieIsBlocked(MaterialLocationKind.InputCassette, true, true));
        Case("output_parent_and_inspection_trace_are_preserved", OutputTraceabilityIsPreserved);
        Case("picker_product_data_gate_ignores_valid_output_history", PickerProductDataGateUsesCurrentAndReservationOnly);
        Case("ready_wafer_blocks_normal_exchange_only", () => FinishGate(WaferMaterialState.Ready));
        Case("work_ready_wafer_blocks_normal_exchange_only", () => FinishGate(WaferMaterialState.WorkReady));
        Case("working_wafer_blocks_normal_exchange_only", () => FinishGate(WaferMaterialState.Working));
        Case("finish_wafer_on_stage_blocks", () => InternalWaferIsBlocked(MaterialLocationKind.InputStage));
        Case("finish_wafer_on_feeder_blocks", () => InternalWaferIsBlocked(MaterialLocationKind.InputFeeder));
        Case("output_linked_die_on_picker_blocks", () => ActiveDieIsBlocked(MaterialLocationKind.PickerRear, false, true));
        Case("finish_exchange_rechecks_state_before_mutation", FinishExchangeAndRecheck);
        Console.WriteLine("RESULT cases={0}, assertions={1}, failures={2}; current source + in-memory save/log, no equipment or operational storage.", _cases, _assertions, _failures);
        return _failures == 0 ? 0 : 1;
    }
}
